using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
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

        private readonly Dictionary<uint, GattCharacteristic> _indexedChars;

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
            [NotNull] IBluetoothDeviceInfo aInfo,
            [NotNull] BluetoothLEDevice aDevice,
            [NotNull] GattCharacteristic[] aChars)
        {
            _bpLogger = aLogManager.GetLogger(GetType());
            _bleDevice = aDevice;

            if (aInfo.Characteristics.Count > 0)
            {
                foreach (var item in aInfo.Characteristics)
                {
                    var c = (from x in aChars
                             where x.Uuid == item.Value
                             select x).ToArray();
                    if (c.Length != 1)
                    {
                        var err = $"Cannot find characteristic ${item.Value} for device {Name}";
                        _bpLogger.Error(err);
                        throw new Exception(err);
                    }

                    if (_indexedChars == null)
                    {
                        _indexedChars = new Dictionary<uint, GattCharacteristic>();
                    }

                    _indexedChars.Add(item.Key, c[0]);
                }
            }
            else if (aChars.Length <= 2)
            {
                foreach (var c in aChars)
                {
                    if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read) ||
                        c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                    {
                        _rxChar = c;
                    }
                    else if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse) ||
                             c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))

                    {
                        _txChar = c;
                    }
                }
            }
            else
            {
                var err = $"No characteristics to connect to for device {Name}";
                _bpLogger.Error(err);
                throw new Exception(err);
            }

            _bleDevice.ConnectionStatusChanged += ConnectionStatusChangedHandler;
        }

        public async Task SubscribeToUpdates()
        {
            if (_rxChar != null && _rxChar.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                GattCommunicationStatus status = await _rxChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status == GattCommunicationStatus.Success)
                {
                    // Server has been informed of clients interest.
                }
            }
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
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"WriteValue using txChar called with no txChar available");
            }

            return await WriteValue(aMsgId, _txChar, aValue, aWriteWithResponse);
        }

        [ItemNotNull]
        public async Task<ButtplugMessage> WriteValue(uint aMsgId,
            uint aIndex,
            byte[] aValue,
            bool aWriteWithResponse = false)
        {
            if (_indexedChars == null)
            {
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"WriteValue using indexed characteristics called with no indexed characteristics available");
            }

            if (!_indexedChars.ContainsKey(aIndex))
            {
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"WriteValue using indexed characteristics called with invalid index");
            }

            // We need the GattCharacteristic cast, otherwise the parameters are ambiguous. I have no
            // idea how GattCharacteristic implicitly casts to Guid but here we are.
            return await WriteValue(aMsgId, _indexedChars[aIndex], aValue, aWriteWithResponse);
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

            try
            {
                _currentTask = aChar.WriteValueAsync(aValue.AsBuffer(),
                    aWriteWithResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse);
                var status = await _currentTask;
                _currentTask = null;
                if (status != GattCommunicationStatus.Success)
                {
                    return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                        $"GattCommunication Error: {status}");
                }
            }
            catch (InvalidOperationException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of
                // a transfer.
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"GattCommunication Error: {e.Message}");
            }
            catch (TaskCanceledException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of
                // a transfer (happened when MysteryVibe lost power).
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"Device disconnected: {e.Message}");
            }

            return new Ok(aMsgId);
        }

        public async Task<(ButtplugMessage, byte[])> ReadValue(uint aMsgId)
        {
            if (_rxChar == null)
            {
                return (_bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"ReadValue using rxChar called with no rxChar available"), new byte[] { });
            }

            return await ReadValue(aMsgId, _rxChar);
        }

        public async Task<(ButtplugMessage, byte[])> ReadValue(uint aMsgId, uint aIndex)
        {
            if (_indexedChars == null)
            {
                return (_bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"ReadValue using indexed characteristics called with no indexed characteristics available"), new byte[] { });
            }

            if (!_indexedChars.ContainsKey(aIndex))
            {
                return (_bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"ReadValue using indexed characteristics called with invalid index"), new byte[] { });
            }

            return await ReadValue(aMsgId, _indexedChars[aIndex]);
        }

        private async Task<(ButtplugMessage, byte[])> ReadValue(uint aMsgId, GattCharacteristic aChar)
        {
            var result = aChar.ReadValueAsync().GetResults().Value.ToArray();
            return (new Ok(aMsgId), result);
        }

        public void Disconnect()
        {
            DeviceRemoved?.Invoke(this, new EventArgs());
            _txChar = null;
            _rxChar = null;
            _indexedChars.Clear();

            _bleDevice.Dispose();
            _bleDevice = null;
        }
    }
}