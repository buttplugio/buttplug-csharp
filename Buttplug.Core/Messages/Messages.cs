using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

// Namespace containing all Buttplug messages, as specified by the Buttplug Message Spec at
// https://docs.buttplug.io/buttplug. For consistency sake, all message descriptions are stated in
// relation to the server, i.e. message are sent "(from client) to server" or "(to client) from server".
namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Signifies the success of the last message/query.
    /// </summary>
    public class Ok : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ok"/> class.
        /// </summary>
        /// <param name="aId">Message ID. Should match the ID of the message being responded to.</param>
        public Ok(uint aId)
            : base(aId)
        {
        }
    }

    /// <summary>
    /// Sent to server, at an interval specified by the server. If ping is not received in a timely
    /// manner, devices are stopped and client/server connection is severed.
    /// </summary>
    // Resharper doesn't seem to be able to deduce that though.
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Ping : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ping"/> class.
        /// </summary>
        /// <param name="aId">Message ID</param>
        public Ping(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    /// <summary>
    /// Sends text to a server, expected to be echoed back.
    /// </summary>
    public class Test : ButtplugMessage
    {
        private string _testStringImpl;

        /// <summary>
        /// Text to send.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="Test"/> class.
        /// </summary>
        /// <param name="aString">Text to send</param>
        /// <param name="aId">Message ID</param>
        public Test(string aString, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
            TestString = aString;
        }
    }

    /// <summary>
    /// Indicator that there has been an error in the system, either due to the last message/query
    /// sent, or due to an internal error.
    /// </summary>
    public class Error : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Types of errors described by the message.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Defined in external spec")]
        public enum ErrorClass
        {
            /// <summary>
            /// Error was caused by unknown factors.
            /// </summary>
            ERROR_UNKNOWN,

            /// <summary>
            /// Error was caused by initilaisaion.
            /// </summary>
            ERROR_INIT,

            /// <summary>
            /// Max ping timeout has been exceeded.
            /// </summary>
            ERROR_PING,

            /// <summary>
            /// Error parsing messages.
            /// </summary>
            ERROR_MSG,

            /// <summary>
            /// Error at the device manager level (device doesn't exist/disconnected, etc...)
            /// </summary>
            ERROR_DEVICE,
        }

        /// <summary>
        /// Specific error type this message describes.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ErrorClass ErrorCode;

        /// <summary>
        /// Human-readable error description
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class. The message ID may be zero
        /// if raised outside of servicing a message from the client, otherwise the message ID must
        /// match the message being serviced.
        /// </summary>
        /// <param name="aErrorMessage">Human-readable error description</param>
        /// <param name="aErrorCode">Class of error</param>
        /// <param name="aId">Message ID</param>
        public Error(string aErrorMessage, ErrorClass aErrorCode, uint aId)
            : base(aId)
        {
            ErrorMessage = aErrorMessage;
            ErrorCode = aErrorCode;
        }
    }

    /// <summary>
    /// Container class for device attributes.
    /// </summary>
    public class MessageAttributes
    {
        /// <summary>
        /// Number of actuators/sensors/channels/etc this message is addressing.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public uint? FeatureCount;
    }

    /// <summary>
    /// Container class for describing a device.
    /// </summary>
    public class DeviceMessageInfo : IButtplugDeviceInfoMessage
    {
        /// <summary>
        /// Name of the device
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        /// <summary>
        /// Device index
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex;

        /// <summary>
        /// List of messages that a device supports, with additional attribute data.
        /// </summary>
        /// While this is set in the constructor, it needs to be initialized here in order to keep
        /// the JSON parser from setting it to null.
        [SuppressMessage("ReSharper", "MemberInitializerValueIgnored", Justification = "JSON.net doesn't use the constructor")]
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, MessageAttributes> DeviceMessages = new Dictionary<string, MessageAttributes>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceMessageInfo"/> class.
        /// </summary>
        /// <param name="aIndex">Device index</param>
        /// <param name="aName">Device name</param>
        /// <param name="aMessages">List of device messages/attributes supported</param>
        public DeviceMessageInfo(uint aIndex, string aName,
            Dictionary<string, MessageAttributes> aMessages)
        {
            DeviceName = aName;
            DeviceIndex = aIndex;
            DeviceMessages = aMessages;
        }

        // Implementation details for IButtplugDeviceInfoMessage interface
        string IButtplugDeviceInfoMessage.DeviceName => DeviceName;

        uint IButtplugDeviceInfoMessage.DeviceIndex => DeviceIndex;

        Dictionary<string, MessageAttributes> IButtplugDeviceInfoMessage.DeviceMessages => DeviceMessages;
    }

    /// <summary>
    /// Container class for describing a device. Represents a prior spec version of the message, kept
    /// for downgrade support.
    /// </summary>
    public class DeviceMessageInfoVersion0
    {
        /// <summary>
        /// Device name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        /// <summary>
        /// Devices index.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex;

        /// <summary>
        /// Commands supported by device.
        /// </summary>
        [SuppressMessage("ReSharper", "MemberInitializerValueIgnored", Justification = "JSON.net doesn't use the constructor")]
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public string[] DeviceMessages = new string[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceMessageInfoVersion0"/> class.
        /// </summary>
        /// <param name="aIndex">Device index</param>
        /// <param name="aName">Device name</param>
        /// <param name="aMessages">Commands supported by device</param>
        public DeviceMessageInfoVersion0(uint aIndex, string aName, string[] aMessages)
        {
            DeviceName = aName;
            DeviceIndex = aIndex;
            DeviceMessages = aMessages;
        }
    }

    /// <summary>
    /// List of devices connected to the server.
    /// </summary>
    public class DeviceList : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// List of devices currently connected.
        /// </summary>
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public readonly DeviceMessageInfo[] Devices = new DeviceMessageInfo[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceList"/> class.
        /// </summary>
        /// <param name="aDeviceList">List of devices currently connected</param>
        /// <param name="aId">Message ID</param>
        public DeviceList(DeviceMessageInfo[] aDeviceList, uint aId)
            : base(aId, 1, typeof(DeviceListVersion0))
        {
            Devices = aDeviceList;
        }

        /// <inheritdoc />
        internal DeviceList()
            : base(0, 1, typeof(DeviceListVersion0))
        {
        }
    }

    /// <summary>
    /// List of devices connected to the server. Represents a prior spec version of the message, kept
    /// for downgrade support.
    /// </summary>
    public class DeviceListVersion0 : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// List of connected devices.
        /// </summary>
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public readonly DeviceMessageInfoVersion0[] Devices = new DeviceMessageInfoVersion0[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceListVersion0"/> class.
        /// </summary>
        /// <param name="aDeviceList">List of connected devices</param>
        /// <param name="aId">Message ID</param>
        public DeviceListVersion0(DeviceMessageInfoVersion0[] aDeviceList, uint aId)
             : base(aId)
        {
            Devices = aDeviceList;
        }

        /// <inheritdoc />
        public DeviceListVersion0()
            : base(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceListVersion0"/> class. Downgrade constructor, for creating a <see cref="DeviceListVersion0"/> from a <see cref="DeviceList"/>
        /// </summary>
        /// <param name="aMsg"><see cref="DeviceList"/> Message to convert to <see cref="DeviceListVersion0"/></param>
        public DeviceListVersion0(DeviceList aMsg)
             : base(aMsg.Id)
        {
            var tmp = new List<DeviceMessageInfoVersion0>();
            foreach (var dev in aMsg.Devices)
            {
                var tmp2 = new List<string>();
                foreach (var k in dev.DeviceMessages.Keys)
                {
                    tmp2.Add(k);
                }

                tmp.Add(new DeviceMessageInfoVersion0(dev.DeviceIndex, dev.DeviceName, tmp2.ToArray()));
            }

            Devices = tmp.ToArray();
        }
    }

    /// <summary>
    /// Sent from server when a new device is discoved.
    /// </summary>
    public class DeviceAdded : ButtplugDeviceMessage, IButtplugMessageOutgoingOnly, IButtplugDeviceInfoMessage
    {
        /// <summary>
        /// Name of device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        /// <summary>
        /// Commands supported by device.
        /// </summary>
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, MessageAttributes> DeviceMessages =
            new Dictionary<string, MessageAttributes>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAdded"/> class.
        /// </summary>
        /// <param name="aIndex">Device index</param>
        /// <param name="aName">Device name</param>
        /// <param name="aMessages">Commands supported by device</param>
        public DeviceAdded(uint aIndex, string aName,
            Dictionary<string, MessageAttributes> aMessages)
            : base(ButtplugConsts.SystemMsgId, aIndex, 1, typeof(DeviceAddedVersion0))
        {
            DeviceName = aName;
            DeviceMessages = aMessages;
        }

        /// <inheritdoc />
        internal DeviceAdded()
            : base(0, 0, 1, typeof(DeviceAddedVersion0))
        {
        }

        // Implementation details for IButtplugDeviceInfoMessage interface
        string IButtplugDeviceInfoMessage.DeviceName => DeviceName;

        uint IButtplugDeviceInfoMessage.DeviceIndex => DeviceIndex;

        Dictionary<string, MessageAttributes> IButtplugDeviceInfoMessage.DeviceMessages => DeviceMessages;
    }

    /// <summary>
    /// Sent from server when a new device is discoved. Represents a prior spec version of the message, kept for downgrade support.
    /// </summary>
    public class DeviceAddedVersion0 : ButtplugDeviceMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Device name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        /// <summary>
        /// Commands supported by device.
        /// </summary>
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public string[] DeviceMessages = new string[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAddedVersion0"/> class.
        /// </summary>
        /// <param name="aIndex">Device index</param>
        /// <param name="aName">Device name</param>
        /// <param name="aMessages">Commands supported by device</param>
        public DeviceAddedVersion0(uint aIndex, string aName, string[] aMessages)
            : base(ButtplugConsts.SystemMsgId, aIndex)
        {
            DeviceName = aName;
            DeviceMessages = aMessages;
        }

        /// <inheritdoc />
        public DeviceAddedVersion0()
            : base(0, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAddedVersion0"/> class. Downgrade constructor, for creating a <see cref="DeviceAddedVersion0"/> from a <see cref="DeviceAdded"/>
        /// </summary>
        /// <param name="aMsg"><see cref="DeviceAdded"/> Message to convert to <see cref="DeviceAddedVersion0"/></param>
        public DeviceAddedVersion0(DeviceAdded aMsg)
            : base(aMsg.Id, aMsg.DeviceIndex)
        {
            DeviceName = aMsg.DeviceName;
            var tmp = new List<string>();
            foreach (var k in aMsg.DeviceMessages.Keys)
            {
                tmp.Add(k);
            }

            DeviceMessages = tmp.ToArray();
        }
    }

    /// <summary>
    /// Sent from server when a device is disconnected.
    /// </summary>
    public class DeviceRemoved : ButtplugMessage, IButtplugMessageOutgoingOnly, IButtplugDeviceInfoMessage
    {
        /// <summary>
        /// Device index.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceRemoved"/> class.
        /// </summary>
        /// <param name="aIndex">Index of disconnected device</param>
        public DeviceRemoved(uint aIndex)
            : base(ButtplugConsts.SystemMsgId)
        {
            DeviceIndex = aIndex;
        }

        // Implementation details for IButtplugDeviceInfoMessage interface
        string IButtplugDeviceInfoMessage.DeviceName => string.Empty;

        uint IButtplugDeviceInfoMessage.DeviceIndex => DeviceIndex;

        Dictionary<string, MessageAttributes> IButtplugDeviceInfoMessage.DeviceMessages => new Dictionary<string, MessageAttributes>();
    }

    /// <summary>
    /// Sent to server to request a list of all connected devices.
    /// </summary>
    public class RequestDeviceList : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDeviceList"/> class.
        /// </summary>
        /// <param name="aId">Message ID</param>
        public RequestDeviceList(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    /// <summary>
    /// Sent to server, to start scanning for devices across supported busses.
    /// </summary>
    public class StartScanning : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartScanning"/> class.
        /// </summary>
        /// <param name="aId">Message ID</param>
        public StartScanning(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    /// <summary>
    /// Sent to server, to stop scanning for devices across supported busses.
    /// </summary>
    public class StopScanning : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopScanning"/> class.
        /// </summary>
        /// <param name="aId">Message ID</param>
        public StopScanning(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    /// <summary>
    /// Sent from server when scanning has finished.
    /// </summary>
    public class ScanningFinished : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScanningFinished"/> class. ID is always 0.
        /// </summary>
        public ScanningFinished()
            : base(ButtplugConsts.SystemMsgId)
        {
        }
    }

    /// <summary>
    /// Sent to server to request log entries be relayed to client.
    /// </summary>
    public class RequestLog : ButtplugMessage
    {
        /// <summary>
        /// Level of log detail that should be relayed.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ButtplugLogLevel LogLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestLog"/> class.
        /// </summary>
        /// <param name="aLogLevel">Log level the server should sent</param>
        /// <param name="aId">Message ID</param>
        public RequestLog(string aLogLevel, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
            if (!Enum.TryParse(aLogLevel, out ButtplugLogLevel level))
            {
                throw new ArgumentException("Invalid log level");
            }

            LogLevel = level;
        }

        /// <inheritdoc />
        public RequestLog(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId) => LogLevel = ButtplugLogLevel.Off;

        /// <inheritdoc />
        public RequestLog()
            : base(ButtplugConsts.DefaultMsgId)
        {
            Id = ButtplugConsts.DefaultMsgId;
            LogLevel = ButtplugLogLevel.Off;
        }
    }

    /// <summary>
    /// Sent from server when logs have been requested. Contains a single log entry.
    /// </summary>
    public class Log : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Log level of message.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ButtplugLogLevel LogLevel;

        /// <summary>
        /// Log message.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string LogMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log"/> class.
        /// </summary>
        /// <param name="aLogLevel">Log level</param>
        /// <param name="aLogMessage">Log message</param>
        public Log(ButtplugLogLevel aLogLevel, string aLogMessage)
            : base(ButtplugConsts.SystemMsgId)
        {
            LogLevel = aLogLevel;
            LogMessage = aLogMessage;
        }
    }

    /// <summary>
    /// Sent to server to set up client information, including client name and schema version.
    /// Denotes the beginning of a conneciton handshake.
    /// </summary>
    public class RequestServerInfo : ButtplugMessage
    {
        /// <summary>
        /// Client name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ClientName;

        /// <summary>
        /// Client message schema version.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public uint MessageVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestServerInfo"/> class.
        /// </summary>
        /// <param name="aClientName">Client name</param>
        /// <param name="aId">Message Id</param>
        /// <param name="aSchemaVersion">Message schema version</param>
        public RequestServerInfo(string aClientName, uint aId = ButtplugConsts.DefaultMsgId, uint aSchemaVersion = CurrentSchemaVersion)
            : base(aId)
        {
            ClientName = aClientName;
            MessageVersion = aSchemaVersion;
        }
    }

    /// <summary>
    /// Sent from server, in response to <see cref="RequestServerInfo"/>. Contains server name,
    /// message version, ping information, etc...
    /// </summary>
    public class ServerInfo : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Product major version.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int MajorVersion;

        /// <summary>
        /// Product minor version.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int MinorVersion;

        /// <summary>
        /// Product build version.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int BuildVersion;

        /// <summary>
        /// The schema version of the server. Must be greater or equal to version client reported in <see cref="RequestServerInfo"/>.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint MessageVersion;

        /// <summary>
        /// Expected ping time (in milliseconds).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint MaxPingTime;

        /// <summary>
        /// Server name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ServerName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerInfo"/> class.
        /// </summary>
        /// <param name="aServerName">Server name</param>
        /// <param name="aMessageVersion">Server message schema version</param>
        /// <param name="aMaxPingTime">Ping timeout</param>
        /// <param name="aId">Message ID</param>
        public ServerInfo(string aServerName, uint aMessageVersion, uint aMaxPingTime, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
            ServerName = aServerName;
            MessageVersion = aMessageVersion;
            MaxPingTime = aMaxPingTime;
            var assembly = Assembly.GetAssembly(typeof(ServerInfo));
            MajorVersion = assembly.GetName().Version.Major;
            MinorVersion = assembly.GetName().Version.Minor;
            BuildVersion = assembly.GetName().Version.Build;
        }
    }

    /// <summary>
    /// Sent to server, denotes commands for devices that can take Fleshlight Launch Firmware v1.2
    /// style messages. See https://docs.buttplug.io/stphikal for protocol info.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class FleshlightLaunchFW12Cmd : ButtplugDeviceMessage
    {
        private uint _speedImpl;

        /// <summary>
        /// Gets or sets the speed at which the fleshlight should move (0-99).
        /// </summary>
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

        /// <summary>
        /// Gets or sets the position the fleshlight should move to (0-99).
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="FleshlightLaunchFW12Cmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">Device index</param>
        /// <param name="aSpeed">Speed to move the fleshlight (0-99)</param>
        /// <param name="aPosition">Position to move the fleshlight to (0-99)</param>
        /// <param name="aId">Message ID</param>
        public FleshlightLaunchFW12Cmd(uint aDeviceIndex, uint aSpeed, uint aPosition, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Speed = aSpeed;
            Position = aPosition;
        }
    }

    /// <summary>
    /// Sent to server, denotes commands for devices that can take Lovense style messages. See
    /// https://docs.buttplug.io/stphikal for protocol info.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class LovenseCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Command string to send to the Lovense device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Command;

        /// <summary>
        /// Initializes a new instance of the <see cref="LovenseCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">Device index</param>
        /// <param name="aDeviceCmd">Lovense-formatted command string</param>
        /// <param name="aId">Message ID</param>
        public LovenseCmd(uint aDeviceIndex, string aDeviceCmd, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Command = aDeviceCmd;
        }
    }

    /// <summary>
    /// Sent to server, denotes commands for devices that can take Kiiroo (Generation 1) style
    /// messages. See https://docs.buttplug.io/stphikal for protocol info.
    /// </summary>
    public class KiirooCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// The Kiiroo command (in string form).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Command;

        /// <summary>
        /// Gets or sets the Kiiroo command (in numeric form).
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="KiirooCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">Device index</param>
        /// <param name="aPosition">Position or vibration speed (0-4)</param>
        /// <param name="aId">Message ID</param>
        public KiirooCmd(uint aDeviceIndex, uint aPosition, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Position = aPosition;
        }
    }

    /// <summary>
    /// Sent to server, denotes commands for devices that can take Vorze A10 Cyclone style messages.
    /// See https://docs.buttplug.io/stphikal for protocol info.
    /// </summary>
    public class VorzeA10CycloneCmd : ButtplugDeviceMessage
    {
        private uint _speedImpl;

        /// <summary>
        /// Gets or sets the speed to rotate
        /// </summary>
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

        /// <summary>
        /// The rotation direction
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool Clockwise;

        /// <summary>
        /// Initializes a new instance of the <see cref="VorzeA10CycloneCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">Device Index</param>
        /// <param name="aSpeed">Rotation speed (0-99)</param>
        /// <param name="aClockwise">Direction to rotate</param>
        /// <param name="aId">Message ID</param>
        public VorzeA10CycloneCmd(uint aDeviceIndex, uint aSpeed, bool aClockwise, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Speed = aSpeed;
            Clockwise = aClockwise;
        }
    }

    /// <summary>
    /// Sent to server, generic message that can control any vibrating device. If sent to device with
    /// multiple vibrators, causes all vibrators to vibrate at same speed. This has been superceeded
    /// by <see cref="VibrateCmd"/>.
    /// </summary>
    public class SingleMotorVibrateCmd : ButtplugDeviceMessage
    {
        private double _speedImpl;

        /// <summary>
        /// Gets or sets vibration speed (0.0-1.0)
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleMotorVibrateCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">Device index</param>
        /// <param name="aSpeed">Vibration speed (0.0-1.0)</param>
        /// <param name="aId">Message ID</param>
        public SingleMotorVibrateCmd(uint aDeviceIndex, double aSpeed, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Speed = aSpeed;
        }
    }

    /// <summary>
    /// Sent to server, generic message that can control any vibrating device. Unlike <see
    /// cref="SingleMotorVibrateCmd"/>, this message can take multiple commands for devices with
    /// multiple vibrators.
    /// </summary>
    public class VibrateCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Container object for representing a single vibration speed on a device that may have
        /// multiple independent vibtators.
        /// </summary>
        public class VibrateSubcommand
        {
            private double _speedImpl;

            /// <summary>
            /// Index of vibrator on device. Indexes are specific per device, see
            /// https://docs.buttplug.io/stphikal for device info.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public uint Index;

            /// <summary>
            /// Gets/sets vibration speed (0.0-1.0)
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public double Speed
            {
                get => _speedImpl;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentException("VibrateCmd Speed cannot be less than 0!");
                    }

                    if (value > 1)
                    {
                        throw new ArgumentException("VibrateCmd Speed cannot be greater than 1!");
                    }

                    _speedImpl = value;
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="VibrateSubcommand"/> class.
            /// </summary>
            /// <param name="aIndex">Vibration feature index</param>
            /// <param name="aSpeed">Vibration speed</param>
            public VibrateSubcommand(uint aIndex, double aSpeed)
            {
                Index = aIndex;
                Speed = aSpeed;
            }
        }

        public static VibrateCmd Create(uint aDeviceIndex, uint aMsgId, double aSpeed, uint aCmdCount)
        {
            return Create(aDeviceIndex, aMsgId, Enumerable.Repeat(aSpeed, (int)aCmdCount).ToArray(), aCmdCount);
        }

        public static VibrateCmd Create(uint aDeviceIndex, uint aMsgId, IEnumerable<double> aSpeeds, uint aCmdCount)
        {
            if (aCmdCount != aSpeeds.Count())
            {
                throw new ArgumentException("Number of speeds and number of commands must match.");
            }

            var cmdList = new List<VibrateSubcommand>((int)aCmdCount);
            // TODO This pattern sucks
            uint i = 0;
            foreach (var speed in aSpeeds)
            {
                cmdList.Add(new VibrateSubcommand(i, speed));
                ++i;
            }

            return new VibrateCmd(aDeviceIndex, cmdList, aMsgId);
        }

        /// <summary>
        /// List of vibrator speeds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<VibrateSubcommand> Speeds;

        /// <summary>
        /// Initializes a new instance of the <see cref="VibrateCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">Device index</param>
        /// <param name="aSpeeds">List of per-vibrator speed commands</param>
        /// <param name="aId">Message ID</param>
        public VibrateCmd(uint aDeviceIndex, List<VibrateSubcommand> aSpeeds, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex, 1)
        {
            Speeds = aSpeeds;
        }
    }

    /// <summary>
    /// Sent to server, generic message that can control any rotating device. This message can take
    /// multiple commands for devices with multiple rotators.
    /// </summary>
    public class RotateCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Container object for representing a single rotation command on a device that may have
        /// multiple independent rotating features.
        /// </summary>
        public class RotateSubcommand
        {
            private double _speedImpl;

            /// <summary>
            /// Index of rotation feature on device. Indexes are specific per device, see
            /// https://docs.buttplug.io/stphikal for device info.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public uint Index;

            /// <summary>
            /// Gets/sets rotation speed.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public double Speed
            {
                get => _speedImpl;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentException("VibrateCmd Speed cannot be less than 0!");
                    }

                    if (value > 1)
                    {
                        throw new ArgumentException("VibrateCmd Speed cannot be greater than 1!");
                    }

                    _speedImpl = value;
                }
            }

            /// <summary>
            /// Rotation direction.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public bool Clockwise;

            /// <summary>
            /// Initializes a new instance of the <see cref="RotateSubcommand"/> class.
            /// </summary>
            /// <param name="aIndex">Rotation feature index</param>
            /// <param name="aSpeed">Rotation speed</param>
            /// <param name="aClockwise">Rotation direction</param>
            public RotateSubcommand(uint aIndex, double aSpeed, bool aClockwise)
            {
                Index = aIndex;
                Speed = aSpeed;
                Clockwise = aClockwise;
            }
        }

        public static RotateCmd Create(uint aDeviceIndex, uint aMsgId, double aSpeed, bool aClockwise, uint aCmdCount)
        {
            return Create(aDeviceIndex, aMsgId, Enumerable.Repeat((aSpeed, aClockwise), (int)aCmdCount), aCmdCount);
        }

        public static RotateCmd Create(uint aDeviceIndex, uint aMsgId, IEnumerable<(double speed, bool clockwise)> aCmds, uint aCmdCount)
        {
            if (aCmdCount != aCmds.Count())
            {
                throw new ArgumentException("Number of speeds and number of commands must match.");
            }
            var cmdList = new List<RotateSubcommand>((int)aCmdCount);
            uint i = 0;
            foreach (var cmd in aCmds)
            {
                cmdList.Add(new RotateSubcommand(i, cmd.speed, cmd.clockwise));
            }
            return new RotateCmd(aDeviceIndex, cmdList, aMsgId);
        }

        /// <summary>
        /// List of rotation speeds and directions.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<RotateSubcommand> Rotations;

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">Device index</param>
        /// <param name="aRotations">List of rotations</param>
        /// <param name="aId">Message ID</param>
        public RotateCmd(uint aDeviceIndex, List<RotateSubcommand> aRotations, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex, 1)
        {
            Rotations = aRotations;
        }
    }

    /// <summary>
    /// Sent to server, generic message that can control any linear-actuated device. This message can
    /// take multiple commands for devices with multiple actuators.
    /// </summary>
    public class LinearCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Container object for representing a single linear motion command on a device that may
        /// have multiple independent linear actuated features.
        /// </summary>
        public class VectorSubcommand
        {
            private double _positionImpl;

            /// <summary>
            /// Index of actuator feature on device. Indexes are specific per device, see
            /// https://docs.buttplug.io/stphikal for device info.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public uint Index;

            /// <summary>
            /// Duration of movement to goal position.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public uint Duration;

            /// <summary>
            /// Gets/sets actuator goal position (0.0-1.0)
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public double Position
            {
                get => _positionImpl;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentException("VibrateCmd Speed cannot be less than 0!");
                    }

                    if (value > 1)
                    {
                        throw new ArgumentException("VibrateCmd Speed cannot be greater than 1!");
                    }

                    _positionImpl = value;
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="VectorSubcommand"/> class.
            /// </summary>
            /// <param name="aIndex">Linear actuator index</param>
            /// <param name="aDuration">Duration of movement</param>
            /// <param name="aPosition">Goal position</param>
            public VectorSubcommand(uint aIndex, uint aDuration, double aPosition)
            {
                Index = aIndex;
                Duration = aDuration;
                Position = aPosition;
            }
        }

        public static LinearCmd Create(uint aDeviceIndex, uint aMsgId, uint aDuration, double aPosition, uint aCmdCount)
        {
            return Create(aDeviceIndex, aMsgId, Enumerable.Repeat((aDuration, aPosition), (int)aCmdCount), aCmdCount);
        }

        public static LinearCmd Create(uint aDeviceIndex, uint aMsgId, IEnumerable<(uint duration, double position)> aCmds, uint aCmdCount)
        {
            if (aCmdCount != aCmds.Count())
            {
                throw new ArgumentException("Number of speeds and number of commands must match.");
            }
            var cmdList = new List<VectorSubcommand>((int)aCmdCount);
            uint i = 0;
            foreach (var cmd in aCmds)
            {
                cmdList.Add(new VectorSubcommand(i, cmd.duration, cmd.position));
                ++i;
            }

            return new LinearCmd(aDeviceIndex, cmdList, aMsgId);
        }

        /// <summary>
        /// List of linear movement vectors.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<VectorSubcommand> Vectors;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">Device index</param>
        /// <param name="aVectors">Movement vector list</param>
        /// <param name="aId">Message ID</param>
        public LinearCmd(uint aDeviceIndex, List<VectorSubcommand> aVectors, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex, 1)
        {
            Vectors = aVectors;
        }
    }

    /// <summary>
    /// Sent to server, stops actions of a specific device.
    /// </summary>
    public class StopDeviceCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopDeviceCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">Device index</param>
        /// <param name="aId">Message ID</param>
        public StopDeviceCmd(uint aDeviceIndex, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
        }
    }

    /// <summary>
    /// Sent to server, stops actions of all currently connected devices.
    /// </summary>
    public class StopAllDevices : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopAllDevices"/> class.
        /// </summary>
        /// <param name="aId">Message ID</param>
        public StopAllDevices(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }
}