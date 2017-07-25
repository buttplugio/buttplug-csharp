using System;
using System.Reflection;
using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    public class Ok : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        public Ok(uint aId)
            : base(aId)
        {
        }
    }

    // Clients may instantiate Ping message, and it is used in pattern matching.
    // Resharper doesn't seem to be able to deduce that though.
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Ping : ButtplugMessage
    {
        public Ping(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    public class Test : ButtplugMessage
    {
        private string _testStringImpl;

        [JsonProperty(Required = Required.Always)]
        public string TestString
        {
            get => _testStringImpl;
            set
            {
                if (value == "Error")
                {
                    throw new ArgumentException("Got an Error Message");
                }

                _testStringImpl = value;
            }
        }

        public Test(string aString, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
            TestString = aString;
        }
    }

    public class Error : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        public enum ErrorClass
        {
            ERROR_UNKNOWN,
            ERROR_INIT,
            ERROR_PING,
            ERROR_MSG,
            ERROR_DEVICE,
        }

        [JsonProperty(Required = Required.Always)]
        public ErrorClass ErrorCode;

        [JsonProperty(Required = Required.Always)]
        public string ErrorMessage;

        public Error(string aErrorMessage, ErrorClass aErrorCode, uint aId)
            : base(aId)
        {
            ErrorMessage = aErrorMessage;
            ErrorCode = aErrorCode;
        }
    }

    public class DeviceMessageInfo
    {
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex;

        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public string[] DeviceMessages = new string[0];

        public DeviceMessageInfo(uint aIndex, string aName, string[] aMessages)
        {
            DeviceName = aName;
            DeviceIndex = aIndex;
            DeviceMessages = aMessages;
        }
    }

    public class DeviceList : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public readonly DeviceMessageInfo[] Devices = new DeviceMessageInfo[0];

        public DeviceList(DeviceMessageInfo[] aDeviceList, uint aId)
             : base(aId)
        {
            Devices = aDeviceList;
        }
    }

    public class DeviceAdded : ButtplugDeviceMessage, IButtplugMessageOutgoingOnly
    {
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public string[] DeviceMessages = new string[0];

        public DeviceAdded(uint aIndex, string aName, string[] aMessages)
            : base(ButtplugConsts.SystemMsgId, aIndex)
        {
            DeviceName = aName;
            DeviceMessages = aMessages;
        }
    }

    public class DeviceRemoved : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex;

        public DeviceRemoved(uint aIndex)
            : base(ButtplugConsts.SystemMsgId)
        {
            DeviceIndex = aIndex;
        }
    }

    public class RequestDeviceList : ButtplugMessage
    {
        public RequestDeviceList(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    public class StartScanning : ButtplugMessage
    {
        public StartScanning(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    public class StopScanning : ButtplugMessage
    {
        public StopScanning(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    public class ScanningFinished : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        public ScanningFinished()
            : base(ButtplugConsts.SystemMsgId)
        {
        }
    }

    public class RequestLog : ButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public ButtplugLogLevel LogLevel;

        // JSON.Net gets angry if it doesn't have a default initializer.
        public RequestLog()
            : base(ButtplugConsts.DefaultMsgId)
        {
        }

        public RequestLog(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId) => LogLevel = ButtplugLogLevel.Off;

        public RequestLog(string aLogLevel, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
            ButtplugLogLevel level;
            if (!Enum.TryParse(aLogLevel, out level))
            {
                throw new ArgumentException("Invalid log level");
            }

            LogLevel = level;
        }
    }

    public class Log : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        [JsonProperty(Required = Required.Always)]
        public ButtplugLogLevel LogLevel;

        [JsonProperty(Required = Required.Always)]
        public string LogMessage;

        public Log(ButtplugLogLevel aLogLevel, string aLogMessage)
            : base(ButtplugConsts.SystemMsgId)
        {
            LogLevel = aLogLevel;
            LogMessage = aLogMessage;
        }
    }

    public class RequestServerInfo : ButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public string ClientName;

        public RequestServerInfo(string aClientName, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
            ClientName = aClientName;
        }
    }

    public class ServerInfo : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        [JsonProperty(Required = Required.Always)]
        public int MajorVersion;

        [JsonProperty(Required = Required.Always)]
        public int MinorVersion;

        [JsonProperty(Required = Required.Always)]
        public int BuildVersion;

        // Disable can be private here, as this field is used for serialization,
        // and may be needed by clients
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        [JsonProperty(Required = Required.Always)]
        public uint MessageVersion;

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        [JsonProperty(Required = Required.Always)]
        public uint MaxPingTime;

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        [JsonProperty(Required = Required.Always)]
        public string ServerName;

        public ServerInfo(string aServerName, uint aMessageVersion, uint aMaxPingTime, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
            ServerName = aServerName;
            MessageVersion = aMessageVersion;
            MaxPingTime = aMaxPingTime;
            MajorVersion = Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Major;
            MinorVersion = Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Minor;
            BuildVersion = Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Build;
        }
    }

    public class FleshlightLaunchFW12Cmd : ButtplugDeviceMessage
    {
        private uint _speedImpl;

        [JsonProperty(Required = Required.Always)]
        public uint Speed
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

        private uint _positionImpl;

        [JsonProperty(Required = Required.Always)]
        public uint Position
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

        public FleshlightLaunchFW12Cmd(uint aDeviceIndex, uint aSpeed, uint aPosition, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Speed = aSpeed;
            Position = aPosition;
        }
    }

    // ReSharper disable once UnusedMember.Global
    public class LovenseCmd : ButtplugDeviceMessage
    {
        public LovenseCmd(uint aDeviceIndex, string aDeviceCmd, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
        }
    }

    public class KiirooCmd : ButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public string Command;

        [JsonIgnore]
        public uint Position
        {
            get
            {
                if (uint.TryParse(Command, out uint pos) && pos <= 4)
                {
                    return pos;
                }

                return 0;
            }

            set
            {
                if (value > 4)
                {
                    throw new ArgumentException("KiirooRawCmd Position cannot be greater than 4");
                }

                Command = value.ToString();
            }
        }

        public KiirooCmd(uint aDeviceIndex, uint aPosition, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Position = aPosition;
        }
    }

    public class VorzeA10CycloneCmd : ButtplugDeviceMessage
    {
        private uint _speedImpl;

        [JsonProperty(Required = Required.Always)]
        public uint Speed
        {
            get => _speedImpl;
            set
            {
                if (value > 99)
                {
                    throw new ArgumentException("VorzeA10CycloneCmd cannot have a speed higher than 99!");
                }

                _speedImpl = value;
            }
        }

        [JsonProperty(Required = Required.Always)]
        public bool Clockwise;

        public VorzeA10CycloneCmd(uint aDeviceIndex, uint aSpeed, bool aClockwise, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Speed = aSpeed;
            Clockwise = aClockwise;
        }
    }

    public class SingleMotorVibrateCmd : ButtplugDeviceMessage
    {
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

        public SingleMotorVibrateCmd(uint aDeviceIndex, double aSpeed, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Speed = aSpeed;
        }
    }

    public class StopDeviceCmd : ButtplugDeviceMessage
    {
        public StopDeviceCmd(uint aDeviceIndex, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
        }
    }

    public class StopAllDevices : ButtplugMessage
    {
        public StopAllDevices(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }
}