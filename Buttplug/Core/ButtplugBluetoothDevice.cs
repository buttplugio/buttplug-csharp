using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;
using Buttplug.Messages;
using LanguageExt;

namespace Buttplug.Core
{
    internal abstract class ButtplugBluetoothDevice : ButtplugDevice
    {
        protected BluetoothLEDevice BleDevice;
        protected readonly GattCharacteristic _writeChr;
        protected readonly GattCharacteristic _readChr;
        private Option<IAsyncOperation<GattCommunicationStatus>> _currentTask;

        protected ButtplugBluetoothDevice(string aName, 
            BluetoothLEDevice aDevice,
            GattCharacteristic aWriteChr,
            GattCharacteristic aReadChr) :
            base(aName)
        {
            BleDevice = aDevice;
            _writeChr = aWriteChr;
            _readChr = aReadChr;
            IsDisconnected = false;
            BleDevice.ConnectionStatusChanged += ConnectionStatusChangedHandler;
        }

        public void ConnectionStatusChangedHandler(BluetoothLEDevice device, object o)
        {
            if (BleDevice.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                InvokeDeviceRemoved();
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ButtplugBluetoothDevice);
        }

        public bool Equals(ButtplugBluetoothDevice other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return BleDevice.BluetoothAddress == other.GetAddress();
        }

        public ulong GetAddress()
        {
            return BleDevice.BluetoothAddress;
        }

        public async Task<ButtplugMessage> WriteToDevice(ButtplugMessage aMsg, IBuffer aBuffer)
        {
            if (_currentTask.IsSome)
            {
                return ButtplugUtils.LogErrorMsg(aMsg.Id, BpLogger, "Device is already has a transfer in progress.");
            }
            _currentTask =
                Option<IAsyncOperation<GattCommunicationStatus>>.Some(_writeChr.WriteValueAsync(aBuffer));
            GattCommunicationStatus status = GattCommunicationStatus.Success;
            await _currentTask.IfSomeAsync(async x =>
            {
                status = await x;
            });                    
            _currentTask = new OptionNone();
            if (status != GattCommunicationStatus.Success)
            {
                return ButtplugUtils.LogErrorMsg(aMsg.Id, BpLogger, $"GattCommunication Error: {status}");
            }
            return new Ok(aMsg.Id);
        }

        public virtual async Task<ButtplugMessage> Initialize()
        {
            return new Ok(0);
        }
    }
}