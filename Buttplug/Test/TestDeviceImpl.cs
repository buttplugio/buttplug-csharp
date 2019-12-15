using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Devices;

namespace Buttplug.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    public class TestDeviceImpl : ButtplugDeviceImpl
    {
        public override IEnumerable<string> DeviceEndpoints => new[]
        {
            Endpoints.Rx,
            Endpoints.Tx,
        };

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
        // Maps a string we get as a write to a return packet. Mostly used for Lovense.
        public Dictionary<string, byte[]> ExpectedNotify = new Dictionary<string, byte[]>();

        public event EventHandler ValueWritten;

        public bool Removed;

        private bool _connected;

        public override bool Connected => _connected;

        public TestDeviceImpl(IButtplugLogManager aLogManager, string aName, string aAddress = null)
           : base(aLogManager)
        {
            Name = aName;
            Address = aAddress ?? new Random().Next(0, 100).ToString();
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

        public void AddExpectedNotify(string aNotifyStr, byte[] aValue)
        {
            ExpectedNotify.Add(aNotifyStr, aValue);
        }

        public override Task WriteValueAsyncInternal(byte[] aValue,
            ButtplugDeviceWriteOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            LastWritten.Add(new WriteData()
            {
                Value = (byte[])aValue.Clone(),
                Endpoint = aOptions.Endpoint,
                WriteWithResponse = aOptions.WriteWithResponse,
            });
            ValueWritten?.Invoke(this, new EventArgs());
            try
            {
                var valueStr = Encoding.UTF8.GetString(aValue, 0, aValue.Length);
                if (ExpectedNotify.ContainsKey(valueStr))
                {
                    InvokeDataReceived(new ButtplugDeviceDataEventArgs("tx", ExpectedNotify[valueStr]));
                }
            }
            catch
            {
                // noop.
            }

            return Task.CompletedTask;
        }

        public override Task<byte[]> ReadValueAsyncInternal(ButtplugDeviceReadOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            // todo This doesn't test endpoints!
            var value = ExpectedRead[ExpectedRead.Keys.ToArray()[0]].ElementAt(0);
            ExpectedRead[ExpectedRead.Keys.ToArray()[0]].RemoveAt(0);
            return Task.FromResult(value);
        }

        public override Task SubscribeToUpdatesAsyncInternal(ButtplugDeviceReadOptions aOptions)
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
