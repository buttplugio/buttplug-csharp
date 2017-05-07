using System.Collections.Generic;
using System.Linq;
using Buttplug.Core;
using Newtonsoft.Json;
using LanguageExt;
using NLog;

namespace Buttplug.Messages
{

    public class Ping : IButtplugMessage
    {
        public Option<string> Check() { return new OptionNone(); }
    }

    public class Test : IButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public string TestString { get; set; }

        public Test(string aString)
        {
            TestString = aString;
        }

        public Option<string> Check()
        {
            return TestString == "Error" ? Option<string>.Some("Got an error message!") : new OptionNone();
        }
    }

    public class Error : IButtplugMessageOutgoingOnly
    {
        [JsonProperty(Required = Required.Always)]
        public string ErrorString { get; }

        public Error(string aErrorString)
        {
            ErrorString = aErrorString;
        }

        public Option<string> Check() { return new OptionNone(); }
    }

    public class DeviceMessageInfo
    {
        public string DeviceName { get; }
        public uint DeviceIndex { get; }

        public DeviceMessageInfo(uint aIndex, string aName)
        {
            DeviceName = aName;
            DeviceIndex = aIndex;
        }
    }

    public class DeviceList : IButtplugMessageOutgoingOnly
    {
        public DeviceMessageInfo[] Devices;

        public DeviceList(DeviceMessageInfo[] aDeviceList)
        {
            Devices = aDeviceList;
        }

        public Option<string> Check()
        {
            return new OptionNone();
        }
    }

    public class DeviceAdded : DeviceMessageInfo, IButtplugMessageOutgoingOnly
    {
        public DeviceAdded(uint aIndex, string aName) : base(aIndex, aName)
        {
        }

        public Option<string> Check()
        {
            return new OptionNone();
        }
    }

    public class DeviceRemoved : IButtplugMessageOutgoingOnly
    {
        public uint DeviceIndex { get; }
        public DeviceRemoved(uint aIndex)
        {
            DeviceIndex = aIndex;
        }

        public Option<string> Check()
        {
            return new OptionNone();
        }
    }

    public class RequestDeviceList : ButtplugMessageNoBody
    { }

    public class StartScanning : ButtplugMessageNoBody
    { }

    public class RequestLog : IButtplugMessage
    {
        private static readonly Dictionary<string, NLog.LogLevel> Levels = new Dictionary<string, LogLevel>()
        {
            { "Off", NLog.LogLevel.Off },
            { "Fatal", NLog.LogLevel.Fatal },
            { "Error", NLog.LogLevel.Error },
            { "Warn", NLog.LogLevel.Warn },
            { "Info", NLog.LogLevel.Info },
            { "Debug", NLog.LogLevel.Debug },
            { "Trace", NLog.LogLevel.Trace }
        };
        public LogLevel LogLevelObj;
        [JsonProperty(Required = Required.Always)]
        public string LogLevel { get; set; }

        public RequestLog(string aLogLevel)
        {
            LogLevel = aLogLevel;
            if (Levels.Keys.Contains(LogLevel))
            {
                LogLevelObj = Levels[LogLevel];
            }
        }

        public Option<string> Check()
        {
            if (!Levels.Keys.Contains(LogLevel))
            {
                return Option<string>.Some($"Log level {LogLevel} is not valid.");
            }
            return new OptionNone();
        }
    }

    public class Log : IButtplugMessageOutgoingOnly
    {
        public string LogLevel { get; }
        public string LogMessage { get; }

        public Log(string aLogLevel, string aLogMessage)
        {
            LogLevel = aLogLevel;
            LogMessage = aLogMessage;
        }

        public Option<string> Check()
        {
            return new OptionNone();
        }
    }

    public class StopScanning : ButtplugMessageNoBody
    { }

    public class ScanningFinished : ButtplugMessageNoBody, IButtplugMessageOutgoingOnly
    { }

    public class RequestServerInfo : ButtplugMessageNoBody
    { }

    public class ServerInfo : IButtplugMessageOutgoingOnly
    {
        public uint MajorVersion { get; }
        public uint MinorVersion { get; }

        public ServerInfo(uint aMajorVersion, uint aMinorVersion)
        {
            MajorVersion = aMajorVersion;
            MinorVersion = aMinorVersion;
        }

        public Option<string> Check()
        {
            return new OptionNone();
        }
    }

    public class FleshlightLaunchRawCmd : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }
        [JsonProperty(Required = Required.Always)]
        public readonly ushort Speed;
        [JsonProperty(Required = Required.Always)]
        public readonly ushort Position;

        public FleshlightLaunchRawCmd(uint aDeviceIndex, ushort aSpeed, ushort aPosition)
        {
            DeviceIndex = aDeviceIndex;
            Speed = aSpeed;
            Position = aPosition;
            Check();
        }

        public Option<string> Check()
        {
            if (Speed > 99)
            {
                return Option<string>.Some("FleshlightLaunchRawMessage cannot have a speed higher than 99!");
            }
            if (Position > 99)
            {
                return Option<string>.Some("FleshlightLaunchRawMessage cannot have a position higher than 99!");
            }
            return new OptionNone();
        }
    }

    public class LovenseRawCmd : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; }

        public Option<string> Check()
        {
            return new OptionNone();
        }
    }

    public class SingleMotorVibrateCmd : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }
        [JsonProperty(Required = Required.Always)]
        public double Speed { get; }

        public SingleMotorVibrateCmd(uint d, double speed)
        {
            DeviceIndex = d;
            Speed = speed;
            Check();
        }

        public Option<string> Check()
        {
            if (Speed < 0)
            {
                return Option<string>.Some("SingleMotorVibrateMessage Speed cannot be less than 0!");
            }
            if (Speed > 1)
            {
                return Option<string>.Some("SingleMotorVibrateMessage Speed cannot be greater than 1!");
            }
            return new OptionNone();
        }
    }

}
