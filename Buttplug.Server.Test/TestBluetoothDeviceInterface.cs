using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;

namespace Buttplug.Server.Test
{
    public class TestBluetoothDeviceInterface : IBluetoothDeviceInterface
    {
        public string Name { get; }

        private readonly ulong _address;

        public class WriteData
        {
            public uint MsgId;
            public Guid Characteristic;
            public byte[] Value;
            public bool WriteWithResponse;

            public WriteData(byte[] aValue)
            {
                Value = new byte[aValue.Length];
                aValue.CopyTo(Value, 0);
            }
        }

        public List<WriteData> LastWriten = new List<WriteData>();

        public event EventHandler DeviceRemoved;

        public bool Removed;

        public TestBluetoothDeviceInterface(string aName, ulong aAddress)
        {
            Name = aName;
            _address = aAddress;
            Removed = false;
            DeviceRemoved += (obj, args) => { Removed = true; };
        }

        public Task<ButtplugMessage> WriteValue(uint aMsgId, Guid aCharacteristic, byte[] aValue, bool aWriteWithResponse = false)
        {
            LastWriten.Add(new WriteData(aValue)
            {
                MsgId = aMsgId,
                Characteristic = aCharacteristic,
                WriteWithResponse = aWriteWithResponse,
            });
            return Task.FromResult<ButtplugMessage>(new Ok(aMsgId));
        }

        public ulong GetAddress()
        {
            return _address;
        }

        public void Disconnect()
        {
            DeviceRemoved?.Invoke(this, new EventArgs());
        }
    }
}
