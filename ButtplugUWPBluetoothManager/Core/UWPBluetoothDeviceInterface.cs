using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Buttplug.Messages;
using Buttplug.Core;
using Buttplug.Bluetooth;
using JetBrains.Annotations;

namespace ButtplugUWPBluetoothManager.Core
{
    internal class UWPBluetoothDeviceInterface : IBluetoothDeviceInterface
    {
        public string Name => _bleDevice.Name;
        [NotNull]
        private readonly BluetoothLEDevice _bleDevice;
        [NotNull]
        private readonly GattCharacteristic[] _gattCharacteristics;
        [CanBeNull]
        private IAsyncOperation<GattCommunicationStatus> _currentTask;
        [NotNull]
        private readonly IButtplugLog _bpLogger;
        [CanBeNull]
        public event EventHandler DeviceRemoved;

        public UWPBluetoothDeviceInterface(
            [NotNull] IButtplugLogManager aLogManager,
            [NotNull] BluetoothLEDevice aDevice,
            [NotNull] GattCharacteristic[] aCharacteristics)
        {
            _bpLogger = aLogManager.GetLogger(GetType());
            _bleDevice = aDevice;
            _gattCharacteristics = aCharacteristics;
            _bleDevice.ConnectionStatusChanged += ConnectionStatusChangedHandler;
        }

        private void ConnectionStatusChangedHandler([NotNull] BluetoothLEDevice aDevice, [NotNull] object aObj)
        {
            if (_bleDevice.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                DeviceRemoved?.Invoke(this, new EventArgs());
            }
        }

        public ulong GetAddress()
        {
            return _bleDevice.BluetoothAddress;
        }

        [ItemNotNull]
        public async Task<ButtplugMessage> WriteValue(uint aMsgId, 
            uint aCharacteristicIndex, 
            byte[] aValue)
        {
            if (!(_currentTask is null))
            {
                return _bpLogger.LogErrorMsg(aMsgId, "Device already has a transfer in progress.");
            }
            var gattCharacteristic = _gattCharacteristics[aCharacteristicIndex];
            if (gattCharacteristic == null)
            {
                return _bpLogger.LogErrorMsg(aMsgId, $"Requested character {aCharacteristicIndex} out of range");
            }
            _currentTask = gattCharacteristic.WriteValueAsync(aValue.AsBuffer());
            var status = await _currentTask;
            _currentTask = null;
            if (status != GattCommunicationStatus.Success)
            {
                return _bpLogger.LogErrorMsg(aMsgId, $"GattCommunication Error: {status}");
            }
            return new Ok(aMsgId);
        }

        [ItemNotNull]
        public Task<byte[]> ReadValue(uint aCharacteristicIndex)
        {
            return Task.FromResult(new byte[]{});
        }

        [ItemNotNull]
        public Task<ButtplugMessage> Subscribe(uint aMsgId,
            uint aCharacertisticIndex)
        {
            return Task.FromResult<ButtplugMessage>(_bpLogger.LogErrorMsg(aMsgId, "Not implemented."));
        }

        [ItemNotNull]
        public Task<ButtplugMessage> Unsubscribe(uint aMsgId,
            uint aCharacertisticIndex)
        {
            return Task.FromResult<ButtplugMessage>(_bpLogger.LogErrorMsg(aMsgId, "Not implemented."));
        }
    }
}