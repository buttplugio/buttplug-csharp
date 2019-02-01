using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Devices;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage.Streams;
using Buttplug.Core;
using Endpoints = Buttplug.Devices.Endpoints;

namespace Buttplug.Server.Managers.UwpHidManager
{
    internal class UwpHidDeviceInterface : ButtplugDeviceImpl
    {
        private readonly HidDevice _device;

        private bool _connected = true;

        public override bool Connected => _connected;

        public UwpHidDeviceInterface(IButtplugLogManager aLogManager, HidDevice aDevice)
            : base(aLogManager)
        {
            _device = aDevice;
            Name = _device.ToString();
        }


        public override void Disconnect()
        {
            _connected = false;
            // noop?
        }

        public override async Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            var report = _device.CreateOutputReport();
            var w = new DataWriter();
            w.WriteBytes(aValue);
            report.Data = w.DetachBuffer();
            await _device.SendOutputReportAsync(report);
            return new Ok(aMsgId);
        }

        public override async Task<ButtplugMessage> WriteValueAsync(uint aMsgId, string aEndpointName, byte[] aValue, bool aWriteWithResponse,
            CancellationToken aToken)
        {
            if (aEndpointName != Endpoints.Tx)
            {
                throw new ButtplugDeviceException(BpLogger, "UwpHidDevice doesn't support any write endpoint except the default.", aMsgId);
            }

            return await WriteValueAsync(aMsgId, aValue, aWriteWithResponse, aToken).ConfigureAwait(false);
        }

        public override Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public override Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, string aEndpointName, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public override Task SubscribeToUpdatesAsync()
        {
            throw new NotImplementedException();
        }

        public override Task SubscribeToUpdatesAsync(string aEndpointName)
        {
            throw new NotImplementedException();
        }
    }
}