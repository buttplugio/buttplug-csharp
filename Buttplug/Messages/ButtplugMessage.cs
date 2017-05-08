using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Buttplug.Core;
using Newtonsoft.Json;
using LanguageExt;
using NLog;

namespace Buttplug.Messages
{
    public class Ok : IButtplugMessageOutgoingOnly
    {}

    public class Ping : IButtplugMessage
    {}

    public class Test : IButtplugMessage
    {
        private string TestStringImpl;

        [JsonProperty(Required = Required.Always)]
        public string TestString
        {
            get => TestStringImpl;
            set
            {
                if (value == "Error")
                {
                    throw new ArgumentException("Got an Error Message");
                }
                TestStringImpl = value;
            }
        }

        public Test(string aString)
        {
            TestString = aString;
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
    }

    public class DeviceAdded : DeviceMessageInfo, IButtplugMessageOutgoingOnly
    {
        public DeviceAdded(uint aIndex, string aName) : base(aIndex, aName)
        {
        }
    }

    public class DeviceRemoved : IButtplugMessageOutgoingOnly
    {
        public uint DeviceIndex { get; }
        public DeviceRemoved(uint aIndex)
        {
            DeviceIndex = aIndex;
        }
    }

    public class RequestDeviceList : IButtplugMessage
    { }

    public class StartScanning : IButtplugMessage
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
        private string _logLevelImpl;
        [JsonProperty(Required = Required.Always)] 
        public string LogLevel { get => _logLevelImpl;
            set
            {
                if (value is null || !Levels.Keys.Contains(value))
                {
                    throw new ArgumentException($"Log level {value} is not valid.");
                }
                _logLevelImpl = value;
                LogLevelObj = Levels[_logLevelImpl];
            }
        }

        public RequestLog()
        {
            LogLevel = "Off";
        }

        public RequestLog(string aLogLevel)
        {
            LogLevel = aLogLevel;
        }
        
        public static Either<string, RequestLog> CreateMessage(string LogLevel)
        {
            try
            {
                return new RequestLog(LogLevel);
            }
            catch (ArgumentException e)
            {
                return e.Message;
            }
        }

        public Option<string> Check()
        {
            if (!Levels.Keys.Contains(_logLevelImpl) || LogLevelObj is null)
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
    }

    public class StopScanning : IButtplugMessage
    { }

    public class ScanningFinished : IButtplugMessageOutgoingOnly
    { }

    public class RequestServerInfo : IButtplugMessage
    { }

    public class ServerInfo : IButtplugMessageOutgoingOnly
    {
        public int MajorVersion { get; }
        public int MinorVersion { get; }
        public int BuildVersion { get; }
        public ServerInfo()
        {
            MajorVersion = Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Major;
            MinorVersion = Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Minor;
            BuildVersion = Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Build;
        }
    }

    public class FleshlightLaunchRawCmd : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }

        private ushort _speedImpl;

        [JsonProperty(Required = Required.Always)]
        public ushort Speed
        {
            get => _speedImpl;
            set
            {
                if (value > 99)
                {
                    throw new ArgumentException("FleshlightLaunchRawMessage cannot have a speed higher than 99!");
                }
                _speedImpl = value;
            }
        }

        private ushort _positionImpl;

        [JsonProperty(Required = Required.Always)]
        public ushort Position
        {
            get => _positionImpl;
            set
            {
                if (value > 99)
                {
                    throw new ArgumentException("FleshlightLaunchRawMessage cannot have a position higher than 99!");
                }
                _positionImpl = value;
            }
        }

        public FleshlightLaunchRawCmd(uint aDeviceIndex, ushort aSpeed, ushort aPosition)
        {
            DeviceIndex = aDeviceIndex;
            Speed = aSpeed;
            Position = aPosition;
        }
    }

    public class LovenseRawCmd : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; }
    }

    public class SingleMotorVibrateCmd : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }

        private double _speedImpl;

        [JsonProperty(Required = Required.Always)]
        public double Speed
        {
            get => _speedImpl;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("SingleMotorVibrateMessage Speed cannot be less than 0!");
                }
                if (value > 1)
                {
                    throw new ArgumentException("SingleMotorVibrateMessage Speed cannot be greater than 1!");
                }
                _speedImpl = value;
            }
        }
        
        public SingleMotorVibrateCmd(uint d, double speed)
        {
            DeviceIndex = d;
            Speed = speed;
        }
    }

}
