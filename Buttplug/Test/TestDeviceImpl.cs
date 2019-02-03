using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Devices;

namespace Buttplug.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    public class TestDeviceImpl : ButtplugDeviceImpl
    {
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

        public List<WriteData> LastWritten = new List<WriteData>();
        public Dictionary<string, List<byte[]>> ExpectedRead = new Dictionary<string, List<byte[]>>();

        public event EventHandler ValueWritten;

        public bool Removed;

        private bool _connected;

        public override bool Connected => _connected;

        public TestDeviceImpl(IButtplugLogManager aLogManager, string aName)
           : base(aLogManager)
        {
            Name = aName;
            Address = new Random().Next(0, 100).ToString();
            Removed = false;
            _connected = true;
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

        public override async Task WriteValueAsync(byte[] aValue, CancellationToken aToken)
        {
            await WriteValueAsync(Endpoints.Tx, aValue, false, aToken);
        }

        public override async Task WriteValueAsync(string aEndpointName, byte[] aValue, CancellationToken aToken)
        {
            await WriteValueAsync(aEndpointName, aValue, false, aToken);
        }

        public override async Task WriteValueAsync(byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            await WriteValueAsync(Endpoints.Tx, aValue, aWriteWithResponse, aToken);
        }

        public override Task WriteValueAsync(string aEndpoint, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            LastWritten.Add(new WriteData()
            {
                Value = (byte[])aValue.Clone(),
                Endpoint = aEndpoint,
                WriteWithResponse = aWriteWithResponse,
            });
            ValueWritten?.Invoke(this, new EventArgs());
            return Task.CompletedTask;
        }

        public override Task<byte[]> ReadValueAsync(CancellationToken aToken)
        {
            var value = ExpectedRead[ExpectedRead.Keys.ToArray()[0]].ElementAt(0);
            ExpectedRead[ExpectedRead.Keys.ToArray()[0]].RemoveAt(0);
            return Task.FromResult(value);
        }

        public override async Task<byte[]> ReadValueAsync(string aEndpointName, CancellationToken aToken)
        {
            return await ReadValueAsync(aToken);
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
            _connected = false;
            InvokeDeviceRemoved();
        }
    }
}
