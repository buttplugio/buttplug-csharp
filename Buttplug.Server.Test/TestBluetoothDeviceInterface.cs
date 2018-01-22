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

        public TestBluetoothDeviceInterface(string aName, ulong aAddress)
        {
            Name = aName;
            _address = aAddress;
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

        public event EventHandler DeviceRemoved;

        public void Disconnect()
        {
            throw new NotImplementedException();
        }
    }
}
