// <copyright file="Messages.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// Namespace containing all Buttplug messages, as specified by the Buttplug Message Spec at
// https://docs.buttplug.io/buttplug. For consistency sake, all message descriptions are stated in
// relation to the server, i.e. message are sent "(from client) to server" or "(to client) from server".
namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Signifies the success of the last message/query.
    /// </summary>
    [ButtplugMessageMetadata("Ok", 0)]
    public class Ok : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ok"/> class.
        /// </summary>
        /// <param name="id">Message ID. Should match the ID of the message being responded to.</param>
        public Ok(uint id)
            : base(id)
        {
        }
    }

    /// <summary>
    /// Sends text to a server, expected to be echoed back.
    /// </summary>
    [ButtplugMessageMetadata("Test", 0)]
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
        /// <param name="string">Text to send.</param>
        /// <param name="id">Message ID.</param>
        public Test(string str, uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
            TestString = str;
        }
    }

    /// <summary>
    /// Indicator that there has been an error in the system, either due to the last message/query
    /// sent, or due to an internal error.
    /// </summary>
    [ButtplugMessageMetadata("Error", 0)]
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
            /// Error was caused during connection handshake.
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
        /// Human-readable error description.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class. The message ID may be zero
        /// if raised outside of servicing a message from the client, otherwise the message ID must
        /// match the message being serviced.
        /// </summary>
        /// <param name="errorMessage">Human-readable error description.</param>
        /// <param name="errorCode">Class of error.</param>
        /// <param name="id">Message ID.</param>
        public Error(string errorMessage, ErrorClass errorCode, uint id)
            : base(id)
        {
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Container class for device attributes.
    /// </summary>
    public class MessageAttributes : IEquatable<MessageAttributes>
    {
        /// <summary>
        /// Number of actuators/sensors/channels/etc this message is addressing.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public uint? FeatureCount;

        public MessageAttributes()
        {
        }

        public MessageAttributes(uint featureCount)
        {
            FeatureCount = featureCount;
        }

        public bool Equals(MessageAttributes attrs)
        {
            return FeatureCount == attrs.FeatureCount;
        }
    }

    /// <summary>
    /// Container class for describing a device.
    /// </summary>
    public class DeviceMessageInfo : IButtplugDeviceInfoMessage
    {
        /// <summary>
        /// Name of the device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        /// <summary>
        /// Device index.
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
        /// <param name="index">Device index.</param>
        /// <param name="name">Device name.</param>
        /// <param name="messages">List of device messages/attributes supported.</param>
        public DeviceMessageInfo(uint index, string name,
            Dictionary<string, MessageAttributes> messages)
        {
            DeviceName = name;
            DeviceIndex = index;
            DeviceMessages = messages;
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
        /// <param name="index">Device index.</param>
        /// <param name="name">Device name.</param>
        /// <param name="messages">Commands supported by device.</param>
        public DeviceMessageInfoVersion0(uint index, string name, string[] messages)
        {
            DeviceName = name;
            DeviceIndex = index;
            DeviceMessages = messages;
        }
    }

    /// <summary>
    /// List of devices connected to the server.
    /// </summary>
    [ButtplugMessageMetadata("DeviceList", 1, typeof(DeviceListVersion0))]
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
        /// <param name="deviceList">List of devices currently connected.</param>
        /// <param name="id">Message ID.</param>
        public DeviceList(DeviceMessageInfo[] deviceList, uint id)
            : base(id)
        {
            Devices = deviceList;
        }

        /// <inheritdoc />
        internal DeviceList()
            : base(0)
        {
        }
    }

    /// <summary>
    /// List of devices connected to the server. Represents a prior spec version of the message, kept
    /// for downgrade support.
    /// </summary>
    [ButtplugMessageMetadata("DeviceList", 0)]
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
        /// <param name="deviceList">List of connected devices.</param>
        /// <param name="id">Message ID.</param>
        public DeviceListVersion0(DeviceMessageInfoVersion0[] deviceList, uint id)
             : base(id)
        {
            Devices = deviceList;
        }

        /// <inheritdoc />
        public DeviceListVersion0()
            : base(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceListVersion0"/> class. Downgrade constructor, for creating a <see cref="DeviceListVersion0"/> from a <see cref="DeviceList"/>.
        /// </summary>
        /// <param name="msg"><see cref="DeviceList"/> Message to convert to <see cref="DeviceListVersion0"/>.</param>
        public DeviceListVersion0(DeviceList msg)
             : base(msg.Id)
        {
            var tmp = new List<DeviceMessageInfoVersion0>();
            foreach (var dev in msg.Devices)
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
    /// Sent from server when a new device is discovered.
    /// </summary>
    [ButtplugMessageMetadata("DeviceAdded", 1, typeof(DeviceAddedVersion0))]
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
        /// <param name="index">Device index.</param>
        /// <param name="name">Device name.</param>
        /// <param name="messages">Commands supported by device.</param>
        public DeviceAdded(uint index, string name,
            Dictionary<string, MessageAttributes> messages)
            : base(ButtplugConsts.SystemMsgId, index)
        {
            DeviceName = name;
            DeviceMessages = messages;
        }

        /// <inheritdoc />
        internal DeviceAdded()
            : base(0, 0)
        {
        }

        // Implementation details for IButtplugDeviceInfoMessage interface
        string IButtplugDeviceInfoMessage.DeviceName => DeviceName;

        uint IButtplugDeviceInfoMessage.DeviceIndex => DeviceIndex;

        Dictionary<string, MessageAttributes> IButtplugDeviceInfoMessage.DeviceMessages => DeviceMessages;
    }

    /// <summary>
    /// Sent from server when a new device is discovered. Represents a prior spec version of the message, kept for downgrade support.
    /// </summary>
    [ButtplugMessageMetadata("DeviceAdded", 0)]
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
        /// <param name="index">Device index.</param>
        /// <param name="name">Device name.</param>
        /// <param name="messages">Commands supported by device.</param>
        public DeviceAddedVersion0(uint index, string name, string[] messages)
            : base(ButtplugConsts.SystemMsgId, index)
        {
            DeviceName = name;
            DeviceMessages = messages;
        }

        /// <inheritdoc />
        public DeviceAddedVersion0()
            : base(0, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAddedVersion0"/> class. Downgrade constructor, for creating a <see cref="DeviceAddedVersion0"/> from a <see cref="DeviceAdded"/>.
        /// </summary>
        /// <param name="msg"><see cref="DeviceAdded"/> Message to convert to <see cref="DeviceAddedVersion0"/>.</param>
        public DeviceAddedVersion0(DeviceAdded msg)
            : base(msg.Id, msg.DeviceIndex)
        {
            DeviceName = msg.DeviceName;
            var tmp = new List<string>();
            foreach (var k in msg.DeviceMessages.Keys)
            {
                tmp.Add(k);
            }

            DeviceMessages = tmp.ToArray();
        }
    }

    /// <summary>
    /// Sent from server when a device is disconnected.
    /// </summary>
    [ButtplugMessageMetadata("DeviceRemoved", 0)]
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
        /// <param name="index">Index of disconnected device.</param>
        public DeviceRemoved(uint index)
            : base(ButtplugConsts.SystemMsgId)
        {
            DeviceIndex = index;
        }

        // Implementation details for IButtplugDeviceInfoMessage interface
        string IButtplugDeviceInfoMessage.DeviceName => string.Empty;

        uint IButtplugDeviceInfoMessage.DeviceIndex => DeviceIndex;

        Dictionary<string, MessageAttributes> IButtplugDeviceInfoMessage.DeviceMessages => new Dictionary<string, MessageAttributes>();
    }

    /// <summary>
    /// Sent to server to request a list of all connected devices.
    /// </summary>
    [ButtplugMessageMetadata("RequestDeviceList", 0)]
    public class RequestDeviceList : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDeviceList"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        public RequestDeviceList(uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
        }
    }

    /// <summary>
    /// Sent to server, to start scanning for devices across supported busses.
    /// </summary>
    [ButtplugMessageMetadata("StartScanning", 0)]
    public class StartScanning : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartScanning"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        public StartScanning(uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
        }
    }

    /// <summary>
    /// Sent to server, to stop scanning for devices across supported busses.
    /// </summary>
    [ButtplugMessageMetadata("StopScanning", 0)]
    public class StopScanning : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopScanning"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        public StopScanning(uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
        }
    }

    /// <summary>
    /// Sent from server when scanning has finished.
    /// </summary>
    [ButtplugMessageMetadata("ScanningFinished", 0)]
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
    /// Sent to server to set up client information, including client name and schema version.
    /// Denotes the beginning of a connection handshake.
    /// </summary>
    [ButtplugMessageMetadata("RequestServerInfo", 0)]
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
        /// <param name="clientName">Client name.</param>
        /// <param name="id">Message Id.</param>
        /// <param name="schemversion">Message schema version.</param>
        public RequestServerInfo(string clientName, uint id = ButtplugConsts.DefaultMsgId, uint schemversion = ButtplugConsts.CurrentSpecVersion)
            : base(id)
        {
            ClientName = clientName;
            MessageVersion = schemversion;
        }
    }

    /// <summary>
    /// Sent from server, in response to <see cref="RequestServerInfo"/>. Contains server name,
    /// message version, ping information, etc...
    /// </summary>
    [ButtplugMessageMetadata("ServerInfo", 0)]
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
        /// <param name="serverName">Server name.</param>
        /// <param name="messageVersion">Server message schema version.</param>
        /// <param name="maxPingTime">Ping timeout.</param>
        /// <param name="id">Message ID.</param>
        public ServerInfo(string serverName, uint messageVersion, uint maxPingTime, uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
            ServerName = serverName;
            MessageVersion = messageVersion;
            MaxPingTime = maxPingTime;
            var assembly = Assembly.GetAssembly(typeof(ServerInfo));
            MajorVersion = assembly.GetName().Version.Major;
            MinorVersion = assembly.GetName().Version.Minor;
            BuildVersion = assembly.GetName().Version.Build;
        }
    }

    /// <summary>
    /// Sent to server, at an interval specified by the server. If ping is not received in a timely
    /// manner, devices are stopped and client/server connection is severed.
    /// </summary>
    // Resharper doesn't seem to be able to deduce that though.
    // ReSharper disable once ClassNeverInstantiated.Global
    [ButtplugMessageMetadata("Ping", 0)]
    public class Ping : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ping"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        public Ping(uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
        }
    }

    /// <summary>
    /// Sent to server, denotes commands for devices that can take Fleshlight Launch Firmware v1.2
    /// style messages. See https://docs.buttplug.io/stphikal for protocol info.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [ButtplugMessageMetadata("FleshlightLaunchFW12Cmd", 0)]
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
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="speed">Speed to move the fleshlight (0-99).</param>
        /// <param name="position">Position to move the fleshlight to (0-99).</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public FleshlightLaunchFW12Cmd(uint deviceIndex, uint speed, uint position, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            Speed = speed;
            Position = position;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleshlightLaunchFW12Cmd"/> class.
        /// </summary>
        /// <param name="speed">Speed to move the fleshlight (0-99).</param>
        /// <param name="position">Position to move the fleshlight to (0-99).</param>
        public FleshlightLaunchFW12Cmd(uint speed, uint position)
        {
            Speed = speed;
            Position = position;
        }
    }

    /// <summary>
    /// Sent to server, denotes commands for devices that can take Lovense style messages. See
    /// https://docs.buttplug.io/stphikal for protocol info.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    [ButtplugMessageMetadata("LovenseCmd", 0)]
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
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="deviceCmd">Lovense-formatted command string.</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public LovenseCmd(uint deviceIndex, string deviceCmd, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            Command = deviceCmd;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LovenseCmd"/> class.
        /// </summary>
        /// <param name="deviceCmd">Lovense-formatted command string.</param>
        public LovenseCmd(string deviceCmd)
        {
            Command = deviceCmd;
        }
    }

    /// <summary>
    /// Sent to server, denotes commands for devices that can take Kiiroo (Generation 1) style
    /// messages. See https://docs.buttplug.io/stphikal for protocol info.
    /// </summary>
    [ButtplugMessageMetadata("KiirooCmd", 0)]
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
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="position">Position or vibration speed (0-4).</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public KiirooCmd(uint deviceIndex, uint position, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            Position = position;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KiirooCmd"/> class.
        /// </summary>
        /// <param name="position">Position or vibration speed (0-4).</param>
        public KiirooCmd(uint position)
        {
            Position = position;
        }
    }

    /// <summary>
    /// Sent to server, denotes commands for devices that can take Vorze A10 Cyclone style messages.
    /// See https://docs.buttplug.io/stphikal for protocol info.
    /// </summary>
    [ButtplugMessageMetadata("VorzeA10CycloneCmd", 0)]
    public class VorzeA10CycloneCmd : ButtplugDeviceMessage
    {
        private uint _speedImpl;

        /// <summary>
        /// Gets or sets the speed to rotate.
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
        /// <param name="deviceIndex">Device Index.</param>
        /// <param name="speed">Rotation speed (0-99).</param>
        /// <param name="clockwise">Direction to rotate.</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public VorzeA10CycloneCmd(uint deviceIndex, uint speed, bool clockwise, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            Speed = speed;
            Clockwise = clockwise;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VorzeA10CycloneCmd"/> class.
        /// </summary>
        /// <param name="speed">Rotation speed (0-99).</param>
        /// <param name="clockwise">Direction to rotate.</param>
        public VorzeA10CycloneCmd(uint speed, bool clockwise)
        {
            Speed = speed;
            Clockwise = clockwise;
        }
    }

    /// <summary>
    /// Sent to server, generic message that can control any vibrating device. If sent to device with
    /// multiple vibrators, causes all vibrators to vibrate at same speed. This has been superseded
    /// by <see cref="VibrateCmd"/>.
    /// </summary>
    [ButtplugMessageMetadata("SingleMotorVibrateCmd", 0)]
    public class SingleMotorVibrateCmd : ButtplugDeviceMessage
    {
        private double _speedImpl;

        /// <summary>
        /// Gets or sets vibration speed (0.0-1.0).
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
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="speed">Vibration speed (0.0-1.0).</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public SingleMotorVibrateCmd(uint deviceIndex, double speed, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            Speed = speed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleMotorVibrateCmd"/> class.
        /// </summary>
        /// <param name="speed">Vibration speed (0.0-1.0).</param>
        public SingleMotorVibrateCmd(double speed)
        {
            Speed = speed;
        }
    }

    public class GenericMessageSubcommand
    {
        /// <summary>
        /// Index of vibrator on device. Indexes are specific per device, see
        /// https://docs.buttplug.io/stphikal for device info.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint Index;

        protected GenericMessageSubcommand(uint index)
        {
            Index = index;
        }
    }

    /// <summary>
    /// Sent to server, generic message that can control any vibrating device. Unlike <see
    /// cref="SingleMotorVibrateCmd"/>, this message can take multiple commands for devices with
    /// multiple vibrators.
    /// </summary>
    [ButtplugMessageMetadata("VibrateCmd", 1)]
    public class VibrateCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Container object for representing a single vibration speed on a device that may have
        /// multiple independent vibrators.
        /// </summary>
        public class VibrateSubcommand : GenericMessageSubcommand
        {
            private double _speedImpl;

            /// <summary>
            /// Gets/sets vibration speed (0.0-1.0).
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
            /// <param name="index">Vibration feature index.</param>
            /// <param name="speed">Vibration speed.</param>
            public VibrateSubcommand(uint index, double speed)
            : base(index)
            {
                Speed = speed;
            }
        }

        public static VibrateCmd Create(double speed, uint cmdCount)
        {
            return Create(uint.MaxValue, ButtplugConsts.DefaultMsgId, Enumerable.Repeat(speed, (int)cmdCount).ToArray());
        }

        public static VibrateCmd Create(IEnumerable<double> speeds)
        {
            return Create(uint.MaxValue, ButtplugConsts.DefaultMsgId, speeds);
        }

        public static VibrateCmd Create(uint deviceIndex, uint msgId, double speed, uint cmdCount)
        {
            return Create(deviceIndex, msgId, Enumerable.Repeat(speed, (int)cmdCount).ToArray());
        }

        public static VibrateCmd Create(uint deviceIndex, uint msgId, IEnumerable<double> speeds)
        {
            var cmdList = new List<VibrateSubcommand>(speeds.Count());

            uint i = 0;
            foreach (var speed in speeds)
            {
                cmdList.Add(new VibrateSubcommand(i, speed));
                ++i;
            }

            return new VibrateCmd(deviceIndex, cmdList, msgId);
        }

        /// <summary>
        /// List of vibrator speeds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<VibrateSubcommand> Speeds;

        /// <summary>
        /// Initializes a new instance of the <see cref="VibrateCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="speeds">List of per-vibrator speed commands.</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public VibrateCmd(uint deviceIndex, List<VibrateSubcommand> speeds, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            Speeds = speeds;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VibrateCmd"/> class.
        /// </summary>
        /// <param name="speeds">List of per-vibrator speed commands.</param>
        public VibrateCmd(List<VibrateSubcommand> speeds)
            : this(uint.MaxValue, speeds)
        {
        }
    }

    /// <summary>
    /// Sent to server, generic message that can control any rotating device. This message can take
    /// multiple commands for devices with multiple rotators.
    /// </summary>
    [ButtplugMessageMetadata("RotateCmd", 1)]
    public class RotateCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Container object for representing a single rotation command on a device that may have
        /// multiple independent rotating features.
        /// </summary>
        public class RotateSubcommand : GenericMessageSubcommand
        {
            private double _speedImpl;

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
                        throw new ArgumentException("RotateCmd Speed cannot be less than 0!");
                    }

                    if (value > 1)
                    {
                        throw new ArgumentException("RotateCmd Speed cannot be greater than 1!");
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
            /// <param name="index">Rotation feature index.</param>
            /// <param name="speed">Rotation speed.</param>
            /// <param name="clockwise">Rotation direction.</param>
            public RotateSubcommand(uint index, double speed, bool clockwise)
            : base(index)
            {
                Speed = speed;
                Clockwise = clockwise;
            }
        }

        public static RotateCmd Create(double speed, bool clockwise, uint cmdCount)
        {
            return Create(uint.MaxValue, ButtplugConsts.DefaultMsgId, Enumerable.Repeat((speed, clockwise), (int)cmdCount));
        }

        public static RotateCmd Create(IEnumerable<(double speed, bool clockwise)> cmds)
        {
            return Create(uint.MaxValue, ButtplugConsts.DefaultMsgId, cmds);
        }

        public static RotateCmd Create(uint deviceIndex, uint msgId, double speed, bool clockwise, uint cmdCount)
        {
            return Create(deviceIndex, msgId, Enumerable.Repeat((speed, clockwise), (int)cmdCount));
        }

        public static RotateCmd Create(uint deviceIndex, uint msgId, IEnumerable<(double speed, bool clockwise)> cmds)
        {
            var cmdList = new List<RotateSubcommand>(cmds.Count());
            uint i = 0;
            foreach (var cmd in cmds)
            {
                cmdList.Add(new RotateSubcommand(i, cmd.speed, cmd.clockwise));
                ++i;
            }

            return new RotateCmd(deviceIndex, cmdList, msgId);
        }

        /// <summary>
        /// List of rotation speeds and directions.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<RotateSubcommand> Rotations;

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="rotations">List of rotations.</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public RotateCmd(uint deviceIndex, List<RotateSubcommand> rotations, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            Rotations = rotations;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateCmd"/> class.
        /// </summary>
        /// <param name="rotations">List of rotations.</param>
        public RotateCmd(List<RotateSubcommand> rotations)
            : this(uint.MaxValue, rotations)
        {
        }
    }

    /// <summary>
    /// Sent to server, generic message that can control any linear-actuated device. This message can
    /// take multiple commands for devices with multiple actuators.
    /// </summary>
    [ButtplugMessageMetadata("LinearCmd", 1)]
    public class LinearCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Container object for representing a single linear motion command on a device that may
        /// have multiple independent linear actuated features.
        /// </summary>
        public class VectorSubcommand : GenericMessageSubcommand
        {
            private double _positionImpl;

            /// <summary>
            /// Duration of movement to goal position.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public uint Duration;

            /// <summary>
            /// Gets/sets actuator goal position (0.0-1.0).
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public double Position
            {
                get => _positionImpl;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentException("LinearCmd Speed cannot be less than 0!");
                    }

                    if (value > 1)
                    {
                        throw new ArgumentException("LinearCmd Speed cannot be greater than 1!");
                    }

                    _positionImpl = value;
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="VectorSubcommand"/> class.
            /// </summary>
            /// <param name="index">Linear actuator index.</param>
            /// <param name="duration">Duration of movement.</param>
            /// <param name="position">Goal position.</param>
            public VectorSubcommand(uint index, uint duration, double position)
            : base(index)
            {
                Duration = duration;
                Position = position;
            }
        }

        public static LinearCmd Create(uint duration, double position, uint cmdCount)
        {
            return Create(uint.MaxValue, ButtplugConsts.DefaultMsgId, Enumerable.Repeat((duration, position), (int)cmdCount));
        }

        public static LinearCmd Create(uint deviceIndex, uint msgId, uint duration, double position, uint cmdCount)
        {
            return Create(deviceIndex, msgId, Enumerable.Repeat((duration, position), (int)cmdCount));
        }

        public static LinearCmd Create(IEnumerable<(uint duration, double position)> cmds)
        {
            return Create(uint.MaxValue, ButtplugConsts.DefaultMsgId, cmds);
        }

        public static LinearCmd Create(uint deviceIndex, uint msgId, IEnumerable<(uint duration, double position)> cmds)
        {
            var cmdList = new List<VectorSubcommand>(cmds.Count());
            uint i = 0;
            foreach (var cmd in cmds)
            {
                cmdList.Add(new VectorSubcommand(i, cmd.duration, cmd.position));
                ++i;
            }

            return new LinearCmd(deviceIndex, cmdList, msgId);
        }

        /// <summary>
        /// List of linear movement vectors.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<VectorSubcommand> Vectors;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="vectors">Movement vector list.</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public LinearCmd(uint deviceIndex, List<VectorSubcommand> vectors, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            Vectors = vectors;
        }

        public LinearCmd(List<VectorSubcommand> vectors)
            : this(uint.MaxValue, vectors)
        {
        }
    }

    /// <summary>
    /// Sent to server, stops actions of a specific device.
    /// </summary>
    [ButtplugMessageMetadata("StopDeviceCmd", 0)]
    public class StopDeviceCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopDeviceCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="id">Message ID.</param>
        public StopDeviceCmd(uint deviceIndex = uint.MaxValue, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
        }
    }

    /// <summary>
    /// Sent to server, stops actions of all currently connected devices.
    /// </summary>
    [ButtplugMessageMetadata("StopAllDevices", 0)]
    public class StopAllDevices : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopAllDevices"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        public StopAllDevices(uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
        }
    }
}