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
            var outputCmd = new OutputCommand
            {
                Vibrate = new OutputCommandValue(speed)
            };
            await _handler.SendMessageExpectOk(new OutputCmd(_deviceIndex, _feature.FeatureIndex, outputCmd));
        }
        
        public bool CanOscillate()
        {
            return _feature.Output.ContainsKey(OutputType.Oscillate);
        }
        
        public async Task OscillateAsync(uint speed)
        {
            var outputCmd = new OutputCommand
            {
                Oscillate = new OutputCommandValue(speed)
            };
            await _handler.SendMessageExpectOk(new OutputCmd(_deviceIndex, _feature.FeatureIndex, outputCmd));
        }

        public bool CanRotate()
        {
            return _feature.Output.ContainsKey(OutputType.Rotate);
        }
        
        public async Task RotateAsync(uint speed)
        {
            var outputCmd = new OutputCommand
            {
                Rotate = new OutputCommandValue(speed)
            };
            await _handler.SendMessageExpectOk(new OutputCmd(_deviceIndex, _feature.FeatureIndex, outputCmd));
        }

        public bool CanConstrict()
        {
            return _feature.Output.ContainsKey(OutputType.Constrict);
        }
        
        public async Task ConstrictAsync(uint speed)
        {
            var outputCmd = new OutputCommand
            {
                Constrict = new OutputCommandValue(speed)
            };
            await _handler.SendMessageExpectOk(new OutputCmd(_deviceIndex, _feature.FeatureIndex, outputCmd));
        }
        
        public bool CanHeater()
        {
            return _feature.Output.ContainsKey(OutputType.Heater);
        }
        
        public async Task HeaterAsync(uint speed)
        {
            var outputCmd = new OutputCommand
            {
                Heater = new OutputCommandValue(speed)
            };
            await _handler.SendMessageExpectOk(new OutputCmd(_deviceIndex, _feature.FeatureIndex, outputCmd));
        }
        
        public bool CanSpray()
        {
            return _feature.Output.ContainsKey(OutputType.Spray);
        }
        
        public async Task SprayAsync(uint speed)
        {
            var outputCmd = new OutputCommand
            {
                Spray = new OutputCommandValue(speed)
            };
            await _handler.SendMessageExpectOk(new OutputCmd(_deviceIndex, _feature.FeatureIndex, outputCmd));
        }
        
        public bool CanLed()
        {
            return _feature.Output.ContainsKey(OutputType.Led);
        }
        
        public async Task LedAsync(uint speed)
        {
            var outputCmd = new OutputCommand
            {
                Led = new OutputCommandValue(speed)
            };
            await _handler.SendMessageExpectOk(new OutputCmd(_deviceIndex, _feature.FeatureIndex, outputCmd));
        }
        
        public bool CanPositionWithDuration()
        {
            return _feature.Output.ContainsKey(OutputType.PositionWithDuration);
        }
        
        public async Task PositionWithDurationAsync(uint position, uint duration)
        {
            var outputCmd = new OutputCommand
            {
                PositionWithDuration = new OutputCommandPositionWithDuration {
                    Position = position, 
                    Duration = duration
                }
            };
            await _handler.SendMessageExpectOk(new OutputCmd(_deviceIndex, _feature.FeatureIndex, outputCmd));
        }
        
        public bool CanRotateWithDirection()
        {
            return _feature.Output.ContainsKey(OutputType.RotateWithDirection);
        }
        
        public async Task RotateWithDirectionAsync(uint speed, bool clockwork)
        {
            var outputCmd = new OutputCommand
            {
                RotateWithDirection = new OutputCommandRotationWithDirection {
                    Speed = speed,
                    Clockwise = clockwork
                }
            };
            await _handler.SendMessageExpectOk(new OutputCmd(_deviceIndex, _feature.FeatureIndex, outputCmd));
        }
    }
}