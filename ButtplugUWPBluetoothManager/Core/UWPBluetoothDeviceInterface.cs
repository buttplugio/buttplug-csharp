using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Buttplug.Messages;
using Buttplug.Core;
using LanguageExt;
using Buttplug.Bluetooth;

namespace ButtplugUWPBluetoothManager.Core
{
    internal class UWPBluetoothDeviceInterface : IBluetoothDeviceInterface
    {
        public string Name
        {
            get => _bleDevice.Name;
            set => throw new ArgumentException("Name cannot be set");
        }
        private readonly BluetoothLEDevice _bleDevice;
        private readonly GattCharacteristic[] _gattCharacteristics;
        private Option<IAsyncOperation<GattCommunicationStatus>> _currentTask;
        private readonly IButtplugLog _bpLogger;
        public event EventHandler DeviceRemoved;

        public UWPBluetoothDeviceInterface(
            IButtplugLogManager aLogManager,
            BluetoothLEDevice aDevice,
            GattCharacteristic[] aCharacteristics)
        {
            _bpLogger = aLogManager.GetLogger(GetType());
            _bleDevice = aDevice;
            _gattCharacteristics = aCharacteristics;
            _bleDevice.ConnectionStatusChanged += ConnectionStatusChangedHandler;
        }

        public void ConnectionStatusChangedHandler(BluetoothLEDevice device, object o)
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

        public async Task<ButtplugMessage> WriteValue(uint aMsgId, 
            uint aCharacteristicIndex, 
            byte[] aValue)
        {
            if (_currentTask.IsSome)
            {
                return _bpLogger.LogErrorMsg(aMsgId, "Device already has a transfer in progress.");
            }
            if (aCharacteristicIndex > _gattCharacteristics.Length)
            {
                return _bpLogger.LogErrorMsg(aMsgId, "Charactertistic index out of range.");
            }

            _currentTask =
                Option<IAsyncOperation<GattCommunicationStatus>>.Some(_gattCharacteristics[aCharacteristicIndex].WriteValueAsync(aValue.AsBuffer()));
            var status = GattCommunicationStatus.Success;
            await _currentTask.IfSomeAsync(async x =>
            {
                status = await x;
            });                    
            _currentTask = new OptionNone();
            if (status != GattCommunicationStatus.Success)
            {
                return _bpLogger.LogErrorMsg(aMsgId, $"GattCommunication Error: {status}");
            }
            return new Ok(aMsgId);
        }

        public async Task<byte[]> ReadValue(uint aCharacteristicIndex)
        {
            return new byte[]{};
        }

        public async Task<ButtplugMessage> Subscribe(uint aMsgId,
            uint aCharacertisticIndex)
        {
            return _bpLogger.LogErrorMsg(aMsgId, "Not implemented.");
        }

        public async Task<ButtplugMessage> Unsubscribe(uint aMsgId,
            uint aCharacertisticIndex)
        {
            return _bpLogger.LogErrorMsg(aMsgId, "Not implemented.");
        }
    }
}