using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using JetBrains.Annotations;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace Buttplug.Server.Managers.UWPBluetoothManager
{
    internal class UWPBluetoothDeviceInterface : IBluetoothDeviceInterface
    {
        public string Name => _bleDevice.Name;

        private GattCharacteristic _txChar;

        private GattCharacteristic _rxChar;

        [NotNull]
        private readonly GattCharacteristic[] _namedChars;

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
            [NotNull] GattCharacteristic[] aChars)
        {
            _bpLogger = aLogManager.GetLogger(GetType());
            _bleDevice = aDevice;
            _namedChars = aChars;

            if (aChars.Length <= 2)
            {
                foreach (var c in aChars)
                {
                    if ((c.CharacteristicProperties & GattCharacteristicProperties.Read) != 0 ||
                        (c.CharacteristicProperties & GattCharacteristicProperties.Notify) != 0)
                    {
                        _rxChar = c;
                    }
                    else if ((c.CharacteristicProperties & GattCharacteristicProperties.WriteWithoutResponse) != 0 ||
                             (c.CharacteristicProperties & GattCharacteristicProperties.Write) != 0)

                    {
                        _txChar = c;
                    }
                }
            }

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

        public async Task<ButtplugMessage> WriteValue(uint aMsgId, byte[] aValue, bool aWriteWithResponse = false)
        {
            if (_txChar == null)
            {
                // Throw here
            }

            return await WriteValue(aMsgId, _txChar, aValue, aWriteWithResponse);
        }

        [ItemNotNull]
        public async Task<ButtplugMessage> WriteValue(uint aMsgId,
            Guid aCharacteristic,
            byte[] aValue,
            bool aWriteWithResponse = false)
        {
            var chrs = from x in _namedChars
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

            // We need the GattCharacteristic cast, otherwise the parameters are ambiguous. I have no
            // idea how GattCharacteristic implicitly casts to Guid but here we are.
            return await WriteValue(aMsgId, (GattCharacteristic)gattCharacteristics[0], aValue, aWriteWithResponse);
        }

        private async Task<ButtplugMessage> WriteValue(uint aMsgId,
            GattCharacteristic aChar,
            byte[] aValue,
            bool aWriteWithResponse = false)
        {
            if (!(_currentTask is null))
            {
                _currentTask.Cancel();
                _bpLogger.Error("Cancelling device transfer in progress for new transfer.");
            }

            _currentTask = aChar.WriteValueAsync(aValue.AsBuffer(), aWriteWithResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse);
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
            _txChar = null;
            _rxChar = null;
            for (int i = 0; i < _namedChars.Length; i++)
            {
                _namedChars[i] = null;
            }

            _bleDevice.Dispose();
            _bleDevice = null;
        }
    }
}