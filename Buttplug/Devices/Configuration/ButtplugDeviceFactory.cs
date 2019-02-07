using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;

namespace Buttplug.Devices.Configuration
{
    public class ButtplugDeviceFactory
    {
        public IProtocolConfiguration Config { get; }

        public string ProtocolName => _protocolType.Name;

        private readonly Type _protocolType;

        public ButtplugDeviceFactory(IProtocolConfiguration aConfig, Type aProtocolType)
        {
            Config = aConfig;
            _protocolType = aProtocolType;
        }

        public async Task<IButtplugDevice> CreateDevice(IButtplugLogManager aLogManager, IButtplugDeviceImpl aDevice)
        {
            var device = new ButtplugDevice(aLogManager, _protocolType, aDevice);
            // Run initialization now, just to make sure we're ready to go when we hand the device back.
            // TODO should probably find a better cancellation token for this. Or like, any at all.
            await device.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            return device;
        }
    }
}
