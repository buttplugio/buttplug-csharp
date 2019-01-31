using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Devices;

namespace Buttplug.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    public class TestDeviceImpl : ButtplugDeviceImpl
    {
        public override string Name { get; }

        public class WriteData
        {
            public uint MsgId;
            public byte[] Value;
            public string Endpoint;
            public bool WriteWithResponse;
        }

        public class ReadData
        {
            public byte[] Value;
        }

        public override string Address { get; }
        public override bool Connected { get; }

        public List<WriteData> LastWritten = new List<WriteData>();
        public Dictionary<string, List<byte[]>> ExpectedRead = new Dictionary<string, List<byte[]>>();

        public override event EventHandler DeviceRemoved;

        public event EventHandler ValueWritten;

#pragma warning disable CS0067 // Unused event (We'll use it once we have more notifications)
        public override event EventHandler<ButtplugDeviceDataEventArgs> DataReceived;
#pragma warning restore CS0067

        public bool Removed;

        public TestDeviceImpl(IButtplugLogManager aLogManager, string aName)
           : base(aLogManager)
        {
            Name = aName;
            Address = new Random().Next(0, 100).ToString();
            Removed = false;
            DeviceRemoved += (obj, args) => { Removed = true; };
        }

        public void AddExpectedRead(string aCharacteristicIndex, byte[] aValue)
        {
            if (!ExpectedRead.ContainsKey(aCharacteristicIndex))
            {
                ExpectedRead.Add(aCharacteristicIndex, new List<byte[]>());
            }

            ExpectedRead[aCharacteristicIndex].Add(aValue);
        }

        public override Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            return WriteValueAsync(aMsgId, Endpoints.Tx, aValue, aWriteWithResponse, aToken);
        }

        public override Task<ButtplugMessage> WriteValueAsync(uint aMsgId, string aEndpoint, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            LastWritten.Add(new WriteData()
            {
                Value = (byte[])aValue.Clone(),
                MsgId = aMsgId,
                Endpoint = aEndpoint,
                WriteWithResponse = aWriteWithResponse,
            });
            ValueWritten?.Invoke(this, new EventArgs());
            return Task.FromResult<ButtplugMessage>(new Ok(aMsgId));
        }

        public override Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken)
        {
            var value = ExpectedRead[ExpectedRead.Keys.ToArray()[0]].ElementAt(0);
            ExpectedRead[ExpectedRead.Keys.ToArray()[0]].RemoveAt(0);
            return Task.FromResult<(ButtplugMessage, byte[])>((new Ok(aMsgId), value));
        }

        public override Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, string aEndpointName, CancellationToken aToken)
        {
            return Task.FromResult<(ButtplugMessage, byte[])>((new Ok(aMsgId), new byte[] { }));
        }

        // noop for tests
        public override Task SubscribeToUpdatesAsync()
        {
            return Task.CompletedTask;
        }

        public override Task SubscribeToUpdatesAsync(string aEndpointName)
        {
            return Task.CompletedTask;
        }

        public override void Disconnect()
        {
            DeviceRemoved?.Invoke(this, new EventArgs());
        }
    }
}
