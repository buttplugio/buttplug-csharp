using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Buttplug.DeviceSimulator.PipeMessages
{
    public interface IDeviceSimulatorPipeMessage
    {
    }

    public class ErrorMsg : IDeviceSimulatorPipeMessage
    {
        public string ErrorMessage;

        public ErrorMsg(string aErrMessage)
        {
            ErrorMessage = aErrMessage;
        }
    }

    public class StartScanning : IDeviceSimulatorPipeMessage
    {
        public StartScanning()
        {
        }
    }

    public class Ping : IDeviceSimulatorPipeMessage
    {
        public Ping()
        {
        }
    }

    public class StopScanning : IDeviceSimulatorPipeMessage
    {
        public StopScanning()
        {
        }
    }

    public class FinishedScanning : IDeviceSimulatorPipeMessage
    {
        public FinishedScanning()
        {
        }
    }

    public class DeviceAdded : IDeviceSimulatorPipeMessage
    {
        public string Name;

        public string Id;

        public bool HasLinear;

        public uint VibratorCount;

        public bool HasRotator;

        public DeviceAdded()
        {
        }
    }

    public class DeviceRemoved : IDeviceSimulatorPipeMessage
    {
        public DeviceRemoved()
        {
        }
    }

    internal class StopDevice : IDeviceSimulatorPipeMessage
    {
        public string Id;

        public StopDevice(string aId)
        {
            Id = aId;
        }
    }

    internal class Vibrate : IDeviceSimulatorPipeMessage
    {
        public string Id;
        public double Speed;
        public uint VibratorId;

        public Vibrate(string aId, double aSpeed, uint aVibratorId)
        {
            Id = aId;
            Speed = aSpeed;
            VibratorId = aVibratorId;
        }
    }

    internal class Linear : IDeviceSimulatorPipeMessage
    {
        public string Id;
        public double Speed;
        public double Position;

        public Linear(string aId, double aSpeed, double aPosition)
        {
            Id = aId;
            Speed = aSpeed;
            Position = aPosition;
        }
    }

    internal class Linear2 : IDeviceSimulatorPipeMessage
    {
        public string Id;
        public uint Duration;
        public double Position;

        public Linear2(string aId, uint aDuration, double aPosition)
        {
            Id = aId;
            Duration = aDuration;
            Position = aPosition;
        }
    }

    internal class Rotate : IDeviceSimulatorPipeMessage
    {
        public string Id;
        public uint Speed;
        public bool Clockwise;

        public Rotate(string aId, uint aSpeed, bool aClockwise)
        {
            Id = aId;
            Speed = aSpeed;
            Clockwise = aClockwise;
        }
    }

    public class PipeMessageParser
    {
        private readonly Dictionary<string, Type> _messageTypes;

        public PipeMessageParser()
        {
            Type[] allTypes;
            try
            {
                allTypes = Assembly.GetAssembly(typeof(IDeviceSimulatorPipeMessage)).GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                allTypes = e.Types;
            }

            var messageClasses = from t in allTypes
                                 where t != null && t.IsClass && t.Namespace == "Buttplug.DeviceSimulator.PipeMessages" && typeof(IDeviceSimulatorPipeMessage).IsAssignableFrom(t)
                                 select t;

            var enumerable = messageClasses as Type[] ?? messageClasses.ToArray();
            _messageTypes = new Dictionary<string, Type>();
            enumerable.ToList().ForEach(aMessageType =>
            {
                _messageTypes.Add(aMessageType.Name, aMessageType);
            });
        }

        public string Serialize(IDeviceSimulatorPipeMessage aMsg)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            var o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(aMsg)));
            return o.ToString(Formatting.None);
        }

        public IDeviceSimulatorPipeMessage Deserialize(string aJsonMsg)
        {
            try
            {
                var o = JObject.Parse(aJsonMsg);
                if (!o.Properties().Any())
                {
                    return new ErrorMsg("No message name available");
                }

                var msgName = o.Properties().First().Name;
                if (!_messageTypes.Keys.Any() || !_messageTypes.Keys.Contains(msgName))
                {
                    return new ErrorMsg($"{msgName} is not a valid message class");
                }

                var s = new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Error };

                // This specifically could fail due to object conversion.
                try
                {
                    var r = o[msgName].Value<JObject>();
                    return (IDeviceSimulatorPipeMessage)r.ToObject(_messageTypes[msgName], s);
                }
                catch (InvalidCastException e)
                {
                    return new ErrorMsg($"Could not create message for JSON {aJsonMsg}: {e.Message}");
                }
                catch (JsonSerializationException e)
                {
                    return new ErrorMsg($"Could not create message for JSON {aJsonMsg}: {e.Message}");
                }
            }
            catch (JsonReaderException e)
            {
                return new ErrorMsg($"Not valid JSON: {aJsonMsg} - {e.Message}");
            }
        }
    }
}