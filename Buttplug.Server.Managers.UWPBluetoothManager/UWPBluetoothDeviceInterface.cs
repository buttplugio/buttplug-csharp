using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using JetBrains.Annotations;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace Buttplug.Server.Managers.UWPBluetoothManager
{
    internal class UWPBluetoothDeviceInterface : IBluetoothDeviceInterface
    {
        public string Name => _bleDevice.Name;

        [NotNull]
        private readonly GattCharacteristic[] _gattCharacteristics;

        [NotNull]
        private readonly IButtplugLog _bpLogger;

        [CanBeNull]
        private BluetoothLEDevice _bleDevice;

        [CanBeNull]
        private IAsyncOperation<GattCommunicationStatus> _currentTask;

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
            if (_bleDevice?.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
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
            Guid aCharacteristic,
            byte[] aValue,
            bool aWriteOption = false)
        {
            if (!(_currentTask is null))
            {
                _currentTask.Cancel();
                _bpLogger.Error("Cancelling device transfer in progress for new transfer.");
            }

            var chrs = from x in _gattCharacteristics
                       where x.Uuid == aCharacteristic
                       select x;

            var gattCharacteristics = chrs.ToArray();

            if (!gattCharacteristics.Any())
            {
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE, $"Requested characteristic {aCharacteristic} not found");
            }
            else if (gattCharacteristics.Length > 1)
            {
                _bpLogger.Warn($"Multiple gattCharacteristics for {aCharacteristic} found");
            }

            var gattCharacteristic = gattCharacteristics[0];
            _currentTask = gattCharacteristic.WriteValueAsync(aValue.AsBuffer(), aWriteOption ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse);
            try
            {
                var status = await _currentTask;
                _currentTask = null;
                if (status != GattCommunicationStatus.Success)
                {
                    return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE, $"GattCommunication Error: {status}");
                }
            }
            catch (InvalidOperationException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of a transfer.
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE, $"GattCommunication Error: {e.Message}");
            }

            return new Ok(aMsgId);
        }

        public void Disconnect()
        {
            DeviceRemoved?.Invoke(this, new EventArgs());
            for (int i = 0; i < _gattCharacteristics.Length; i++)
            {
                _gattCharacteristics[i] = null;
            }

            _bleDevice.Dispose();
            _bleDevice = null;
        }
    }
}
