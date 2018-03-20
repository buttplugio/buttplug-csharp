using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// The Ok Buttplug message: the normal response to most other messages.
    /// </summary>
    public class Ok : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ok"/> class.
        /// A message ID is always required as this is in response to another message.
        /// </summary>
        /// <param name="aId">The Message ID</param>
        public Ok(uint aId)
            : base(aId)
        {
        }
    }

    /// <summary>
    /// The Ping Buttplug message: sent by the client at a regular rate to keep the connecton alive.
    /// </summary>
    // Resharper doesn't seem to be able to deduce that though.
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Ping : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ping"/> class.
        /// </summary>
        /// <param name="aId">The message ID</param>
        public Ping(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    /// <summary>
    /// The Test Buttplug message: Sends text to the server to be echo'ed back.
    /// </summary>
    public class Test : ButtplugMessage
    {
        private string _testStringImpl;

        /// <summary>
        /// Gets or sets the text to send.
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
        /// <param name="aString">The text to send</param>
        /// <param name="aId">The message ID</param>
        public Test(string aString, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
            TestString = aString;
        }
    }

    /// <summary>
    /// The Error Buttplug message: An indicator that something has gone wrong.
    /// </summary>
    public class Error : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// The classes of error
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Defined in external spec")]
        public enum ErrorClass
        {
            /// <summary>
            /// The error was casued by unknown factors.
            /// </summary>
            ERROR_UNKNOWN,

            /// <summary>
            /// The error was casued by initilaisaion.
            /// </summary>
            ERROR_INIT,

            /// <summary>
            /// The max ping timeout has been exceeded.
            /// </summary>
            ERROR_PING,

            /// <summary>
            /// There was an error parsing messages.
            /// </summary>
            ERROR_MSG,

            /// <summary>
            /// There was an error at the device manager level.
            /// </summary>
            ERROR_DEVICE,
        }

        /// <summary>
        /// The class of error this message represents.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ErrorClass ErrorCode;

        /// <summary>
        /// The error message (hopefully human readable)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// The message ID may be zero if raised outside of servicing a
        /// message from the client, otherwise the message ID must match
        /// the message being serviced.
        /// </summary>
        /// <param name="aErrorMessage">The error message</param>
        /// <param name="aErrorCode">The class of error</param>
        /// <param name="aId">The message ID</param>
        public Error(string aErrorMessage, ErrorClass aErrorCode, uint aId)
            : base(aId)
        {
            ErrorMessage = aErrorMessage;
            ErrorCode = aErrorCode;
        }
    }

    /// <summary>
    /// This is a coantainer class for device attributes.
    /// New attributes may be added, but never removed.
    /// </summary>
    public class MessageAttributes
    {
        /// <summary>
        /// Number of actuators/sensors/channels/etc this message is addressing
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public uint? FeatureCount;
    }

    /// <summary>
    /// This is a container class for describing a device.
    /// </summary>
    public class DeviceMessageInfo
    {
        /// <summary>
        /// The name of the device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        /// <summary>
        /// The device's index.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex;

        /// <summary>
        /// List of command messages that this device supports with additional attribute data.
        /// </summary>
        [SuppressMessage("ReSharper", "MemberInitializerValueIgnored", Justification = "JSON.net doesn't use the constructor")]
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, MessageAttributes> DeviceMessages = new Dictionary<string, MessageAttributes>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceMessageInfo"/> class.
        /// </summary>
        /// <param name="aIndex">The device's index</param>
        /// <param name="aName">The devices name</param>
        /// <param name="aMessages">The device messages supported</param>
        public DeviceMessageInfo(uint aIndex, string aName,
            Dictionary<string, MessageAttributes> aMessages)
        {
            DeviceName = aName;
            DeviceIndex = aIndex;
            DeviceMessages = aMessages;
        }
    }

    /// <summary>
    /// This is a container class for describing a device.
    /// This is an older version of class, kept for downgrade support.
    /// </summary>
    public class DeviceMessageInfoVersion0
    {
        /// <summary>
        /// The name of the device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        /// <summary>
        /// The device's index.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex;

        /// <summary>
        /// List of command messages that this device supports.
        /// </summary>
        [SuppressMessage("ReSharper", "MemberInitializerValueIgnored", Justification = "JSON.net doesn't use the constructor")]
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public string[] DeviceMessages = new string[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceMessageInfoVersion0"/> class.
        /// </summary>
        /// <param name="aIndex">The device's index</param>
        /// <param name="aName">The devices name</param>
        /// <param name="aMessages">The device messages supported</param>
        public DeviceMessageInfoVersion0(uint aIndex, string aName, string[] aMessages)
        {
            DeviceName = aName;
            DeviceIndex = aIndex;
            DeviceMessages = aMessages;
        }
    }

    /// <summary>
    /// The DeviceList Buttplug message: encapsulates a list of devices connected to the server.
    /// </summary>
    public class DeviceList : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// An array of devices.
        /// </summary>
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public readonly DeviceMessageInfo[] Devices = new DeviceMessageInfo[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceList"/> class.
        /// </summary>
        /// <param name="aDeviceList">The array of devices</param>
        /// <param name="aId">The message ID</param>
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
    /// The DeviceList Buttplug message: encapsulates a list of devices connected to the server.
    /// This is an older version of class, kept for downgrade support.
    /// </summary>
    public class DeviceListVersion0 : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// An array of devices.
        /// </summary>
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public readonly DeviceMessageInfoVersion0[] Devices = new DeviceMessageInfoVersion0[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceListVersion0"/> class.
        /// </summary>
        /// <param name="aDeviceList">The array of devices</param>
        /// <param name="aId">The message ID</param>
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
        /// Initializes a new instance of the <see cref="DeviceListVersion0"/> class.
        /// The downgrade constructor, for creating a <see cref="DeviceListVersion0"/> from a <see cref="DeviceList"/>
        /// </summary>
        /// <param name="aMsg">The message to convert to the older version.</param>
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
    /// The DeviceAdded Buttplug message: sent to the client by the server when a new device is discoved.
    /// </summary>
    public class DeviceAdded : ButtplugDeviceMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// The name of the device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        /// <summary>
        /// List of command messages that this device supports with additional attribute data.
        /// </summary>
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, MessageAttributes> DeviceMessages =
            new Dictionary<string, MessageAttributes>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAdded"/> class.
        /// </summary>
        /// <param name="aIndex">The device's index</param>
        /// <param name="aName">The device's name</param>
        /// <param name="aMessages">The message supported</param>
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
    }

    /// <summary>
    /// The DeviceAdded Buttplug message: sent to the client by the server when a new device is discoved.
    /// This is an older version of class, kept for downgrade support.
    /// </summary>
    public class DeviceAddedVersion0 : ButtplugDeviceMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// The name of the device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        /// <summary>
        /// List of command messages that this device supports.
        /// </summary>
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public string[] DeviceMessages = new string[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAddedVersion0"/> class.
        /// </summary>
        /// <param name="aIndex">The device's index</param>
        /// <param name="aName">The device's name</param>
        /// <param name="aMessages">The message supported</param>
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
        /// Initializes a new instance of the <see cref="DeviceAddedVersion0"/> class.
        /// The downgrade constructor, for creating a <see cref="DeviceAddedVersion0"/> from a <see cref="DeviceAdded"/>
        /// </summary>
        /// <param name="aMsg">The message to convert to the older version.</param>
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
    /// The DeviceRemoved Buttplug message: sent to the client by the server when a new device is disconnected.
    /// </summary>
    public class DeviceRemoved : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// The disconnected device's index.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceRemoved"/> class.
        /// </summary>
        /// <param name="aIndex">The index of the disconnected device</param>
        public DeviceRemoved(uint aIndex)
            : base(ButtplugConsts.SystemMsgId)
        {
            DeviceIndex = aIndex;
        }
    }

    /// <summary>
    /// The RequestDeviceList Buttplug message: sent to the server by the client to request a list of all connected devices.
    /// </summary>
    public class RequestDeviceList : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDeviceList"/> class.
        /// </summary>
        /// <param name="aId">The message ID</param>
        public RequestDeviceList(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    /// <summary>
    /// The StartScanning Buttplug message: request the server starts scanning for devices.
    /// </summary>
    public class StartScanning : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartScanning"/> class.
        /// </summary>
        /// <param name="aId">The message ID</param>
        public StartScanning(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    /// <summary>
    /// The StopScanning Buttplug message: requests the server stops scanning for devices.
    /// </summary>
    public class StopScanning : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopScanning"/> class.
        /// </summary>
        /// <param name="aId">The message ID</param>
        public StopScanning(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }

    /// <summary>
    /// The ScanningFinished Buttplug message: sent to the client by the server when scanning has finished.
    /// </summary>
    public class ScanningFinished : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScanningFinished"/> class.
        /// </summary>
        public ScanningFinished()
            : base(ButtplugConsts.SystemMsgId)
        {
        }
    }

    /// <summary>
    /// The RequestLog Buttplug message: sent to the server by the client to request log entries be sent to the client.
    /// </summary>
    public class RequestLog : ButtplugMessage
    {
        /// <summary>
        /// The level of detail that the server should send to the client.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ButtplugLogLevel LogLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestLog"/> class.
        /// </summary>
        /// <param name="aLogLevel">The log level the server should send.</param>
        /// <param name="aId">The message ID</param>
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
    /// The Log Buttplug message: A log entry
    /// </summary>
    public class Log : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// The log level this entry was raised on.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ButtplugLogLevel LogLevel;

        /// <summary>
        /// The log message (hopefully human readable)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string LogMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log"/> class.
        /// </summary>
        /// <param name="aLogLevel">The log level</param>
        /// <param name="aLogMessage">The  log message</param>
        public Log(ButtplugLogLevel aLogLevel, string aLogMessage)
            : base(ButtplugConsts.SystemMsgId)
        {
            LogLevel = aLogLevel;
            LogMessage = aLogMessage;
        }
    }

    /// <summary>
    /// The RequestServerInfo Buttplug message: beins the protocol handshake
    /// </summary>
    public class RequestServerInfo : ButtplugMessage
    {
        /// <summary>
        /// The client's name (will be shown in the server GUI).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ClientName;

        /// <summary>
        /// The schema version supported by the client.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public uint MessageVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestServerInfo"/> class.
        /// </summary>
        /// <param name="aClientName">The client's name</param>
        /// <param name="aId">The message Id</param>
        /// <param name="aSchemaVersion">The schema version</param>
        public RequestServerInfo(string aClientName, uint aId = ButtplugConsts.DefaultMsgId, uint aSchemaVersion = CurrentSchemaVersion)
            : base(aId)
        {
            ClientName = aClientName;
            MessageVersion = aSchemaVersion;
        }
    }

    /// <summary>
    /// The ServerInfo Buttplug message: the second half of the protocol handshake
    /// </summary>
    public class ServerInfo : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// In the version string X.Y.Z, this is X (this is the server's product version)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int MajorVersion;

        /// <summary>
        /// In the version string X.Y.Z, this is Y (this is the server's product version)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int MinorVersion;

        /// <summary>
        /// In the version string X.Y.Z, this is Z (this is the server's product version)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int BuildVersion;

        /// <summary>
        /// The schema version of the server (must be greater or equal to the client)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint MessageVersion;

        /// <summary>
        /// The time in milliseconds in which the server will wait for a Ping message before disconnecting
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint MaxPingTime;

        /// <summary>
        /// The server's friendly name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ServerName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerInfo"/> class.
        /// </summary>
        /// <param name="aServerName">The server's name</param>
        /// <param name="aMessageVersion">The server's schema version</param>
        /// <param name="aMaxPingTime">The ping timeout</param>
        /// <param name="aId">The message ID</param>
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
    /// The FleshlightLaunchFW12Cmd Buttplug device message: controls a Fleshlight Launch
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
        /// <param name="aDeviceIndex">The index of the device.</param>
        /// <param name="aSpeed">The speed to move the fleshlight (0-99)</param>
        /// <param name="aPosition">The position to move the fleshlight to (0-99)</param>
        /// <param name="aId">The message ID</param>
        public FleshlightLaunchFW12Cmd(uint aDeviceIndex, uint aSpeed, uint aPosition, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Speed = aSpeed;
            Position = aPosition;
        }
    }

    /// <summary>
    /// The LovenseCmd Buttplug device message: controls Lovense devices
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class LovenseCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// The command string to send to the Lovense device.
        /// E.g. "vibrate:10;"
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Command;

        /// <summary>
        /// Initializes a new instance of the <see cref="LovenseCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">The index of the device.</param>
        /// <param name="aDeviceCmd">The command string</param>
        /// <param name="aId">The message ID</param>
        public LovenseCmd(uint aDeviceIndex, string aDeviceCmd, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Command = aDeviceCmd;
        }
    }

    /// <summary>
    /// The KiirooCmd Buttplug device message: controls Gen1 Kiiroo devices
    /// </summary>
    public class KiirooCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// The Kiiroo command (in string form)
        /// Gen1 Kiiroo devices accept 0-4 for vibration strength or position
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Command;

        /// <summary>
        /// Gets or sets the Kiiroo command (in numeric form)
        /// Gen1 Kiiroo devices accept 0-4 for vibration strength or position
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
        /// <param name="aDeviceIndex">The index of the device.</param>
        /// <param name="aPosition">The position or vibration speed (0-4)</param>
        /// <param name="aId">The message ID</param>
        public KiirooCmd(uint aDeviceIndex, uint aPosition, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Position = aPosition;
        }
    }

    /// <summary>
    /// The VorzeA10CycloneCmd Buttplug device message: controls a Vorze A10 Cyclone
    /// </summary>
    public class VorzeA10CycloneCmd : ButtplugDeviceMessage
    {
        private uint _speedImpl;

        /// <summary>
        /// Gets or sets the speed to rotate (0-99)
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
        /// The rotation direction.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool Clockwise;

        /// <summary>
        /// Initializes a new instance of the <see cref="VorzeA10CycloneCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">The index of the device.</param>
        /// <param name="aSpeed">The rotation speed (0-99)</param>
        /// <param name="aClockwise">The direction to rotate in</param>
        /// <param name="aId">The message ID</param>
        public VorzeA10CycloneCmd(uint aDeviceIndex, uint aSpeed, bool aClockwise, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Speed = aSpeed;
            Clockwise = aClockwise;
        }
    }

    /// <summary>
    /// The SingleMotorVibrateCmd Buttplug device message: controls vibrating devices
    /// This has been superceeded by <see cref="VibrateCmd"/>
    /// </summary>
    public class SingleMotorVibrateCmd : ButtplugDeviceMessage
    {
        private double _speedImpl;

        /// <summary>
        /// Gets or sets the speed at which to vibrate at (0.0-1.0)
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
        /// <param name="aDeviceIndex">The index of the device.</param>
        /// <param name="aSpeed">The vibrator speed (0.0-1.0)</param>
        /// <param name="aId">The message ID</param>
        public SingleMotorVibrateCmd(uint aDeviceIndex, double aSpeed, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
            Speed = aSpeed;
        }
    }

    /// <summary>
    /// The VibrateCmd Buttplug device message: controls vibrating devices
    /// </summary>
    public class VibrateCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// The container object for a vibration speed on a device that may
        /// have multiple independent vibtators.
        /// </summary>
        public class VibrateSubcommand
        {
            private double _speedImpl;

            /// <summary>
            /// The index of the vibrator on device
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public uint Index;

            /// <summary>
            /// Gets or sets the speed to set the vibration motor to (0.0-1.0)
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
            /// <param name="aIndex">The vibration motor index</param>
            /// <param name="aSpeed">The vibration speed</param>
            public VibrateSubcommand(uint aIndex, double aSpeed)
            {
                Index = aIndex;
                Speed = aSpeed;
            }
        }

        /// <summary>
        /// A list of vibrator speeds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<VibrateSubcommand> Speeds;

        /// <summary>
        /// Initializes a new instance of the <see cref="VibrateCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">The index of the device.</param>
        /// <param name="aSpeeds">A list of speeds</param>
        /// <param name="aId">The message ID</param>
        public VibrateCmd(uint aDeviceIndex, List<VibrateSubcommand> aSpeeds, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex, 1)
        {
            Speeds = aSpeeds;
        }
    }

    /// <summary>
    /// The RotateCmd Buttplug device message: controls rotating devices
    /// </summary>
    public class RotateCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// The container object for a rotation speeds and directions on a device
        /// that may have multiple independent rotators.
        /// </summary>
        public class RotateSubcommand
        {
            private double _speedImpl;

            /// <summary>
            /// The index of the rotator on device
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public uint Index;

            /// <summary>
            /// Gets or sets the speed to set the vibration motor to (0.0-1.0)
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
            /// Direction to rotate in.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public bool Clockwise;

            /// <summary>
            /// Initializes a new instance of the <see cref="RotateSubcommand"/> class.
            /// </summary>
            /// <param name="aIndex">The rotation motor index</param>
            /// <param name="aSpeed">The rotation speed</param>
            /// <param name="aClockwise">The rotation direction</param>
            public RotateSubcommand(uint aIndex, double aSpeed, bool aClockwise)
            {
                Index = aIndex;
                Speed = aSpeed;
                Clockwise = aClockwise;
            }
        }

        /// <summary>
        /// A list of rotation speeds and directions.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<RotateSubcommand> Rotations;

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">The index of the device.</param>
        /// <param name="aRotations">A list of rotations</param>
        /// <param name="aId">The message ID</param>
        public RotateCmd(uint aDeviceIndex, List<RotateSubcommand> aRotations, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex, 1)
        {
            Rotations = aRotations;
        }
    }

    /// <summary>
    /// The LinearCmd Buttplug device message: controls linear actuator (thrusting) devices
    /// </summary>
    public class LinearCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// The container object for a moement vector on a device that may
        /// have multiple independent linear actuators.
        /// </summary>
        public class VectorSubcommands
        {
            private double _positionImpl;

            /// <summary>
            /// The index of the linear actuator on device
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public uint Index;

            /// <summary>
            /// The time in milliseconds that the device should take to move to the target position
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public uint Duration;

            /// <summary>
            /// Gets or sets the position within the actuators range to move to (0.0-1.0)
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
            /// Initializes a new instance of the <see cref="VectorSubcommands"/> class.
            /// </summary>
            /// <param name="aIndex">The rotation motor index</param>
            /// <param name="aDuration">The duration of movement</param>
            /// <param name="aPosition">The target position</param>
            public VectorSubcommands(uint aIndex, uint aDuration, double aPosition)
            {
                Index = aIndex;
                Duration = aDuration;
                Position = aPosition;
            }
        }

        /// <summary>
        /// A list of vectors.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<VectorSubcommands> Vectors;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">The index of the device.</param>
        /// <param name="aVectors">A list of vectors</param>
        /// <param name="aId">The message ID</param>
        public LinearCmd(uint aDeviceIndex, List<VectorSubcommands> aVectors, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex, 1)
        {
            Vectors = aVectors;
        }
    }

    /// <summary>
    /// The StopDeviceCmd Buttplug device message: stops a specic device
    /// </summary>
    public class StopDeviceCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopDeviceCmd"/> class.
        /// </summary>
        /// <param name="aDeviceIndex">The index of the device.</param>
        /// <param name="aId">The message ID</param>
        public StopDeviceCmd(uint aDeviceIndex, uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId, aDeviceIndex)
        {
        }
    }

    /// <summary>
    /// The StopAllDevices Buttplug message: stops all devices
    /// </summary>
    public class StopAllDevices : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopAllDevices"/> class.
        /// </summary>
        /// <param name="aId">The message ID</param>
        public StopAllDevices(uint aId = ButtplugConsts.DefaultMsgId)
            : base(aId)
        {
        }
    }
}
