using System.Threading.Tasks;
using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    public class ButtplugClientDeviceFeature
    {
        private DeviceFeature _feature;
        private ButtplugClientMessageHandler _handler;
        private uint _deviceIndex;
        
        public FeatureType FeatureType =>  _feature.FeatureType;

        internal ButtplugClientDeviceFeature(uint deviceIndex, DeviceFeature feature, ButtplugClientMessageHandler handler)
        {
            _deviceIndex = deviceIndex;
            _feature = feature;
            _handler = handler;
        }

        public bool CanVibrate()
        {
            return _feature.Output.ContainsKey(OutputType.Vibrate);
        }
        
        public async Task VibrateAsync(uint speed)
        {
            var outputCmd = new OutputCommand();
            outputCmd.Vibrate = new OutputCommandValue(speed);
            await _handler.SendMessageExpectOk(new OutputCmd(_deviceIndex, _feature.FeatureIndex, outputCmd));
        }
    }
}