// <copyright file="Messages.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

// Namespace containing all Buttplug messages, as specified by the Buttplug Message Spec at
// https://docs.buttplug.io/spec. For consistency sake, all message descriptions are stated in
// relation to the server, i.e. message are sent "(from client) to server" or "(to client) from server".
namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Signifies the success of the last message/query.
    /// </summary>
    [ButtplugMessageMetadata("Ok")]
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
    [ButtplugMessageMetadata("Test")]
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
    [ButtplugMessageMetadata("Error")]
    public class Error : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Types of errors described by the message.
        /// </summary>
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
        public readonly string DeviceName;

        /// <summary>
        /// Device index.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly uint DeviceIndex;

        /// <summary>
        /// Device display name, set up by the user.
        /// </summary>
        public readonly string DeviceDisplayName;

        /// <summary>
        /// Recommended amount of time between commands, in milliseconds.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly uint DeviceMessageTimingGap;

        /// <summary>
        /// List of messages that a device supports, with additional attribute data.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly DeviceMessageAttributes DeviceMessages;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceMessageInfo"/> class.
        /// </summary>
        /// <param name="index">Device index.</param>
        /// <param name="name">Device name.</param>
        /// <param name="messages">List of device messages/attributes supported.</param>
        public DeviceMessageInfo(uint index, string name,
            DeviceMessageAttributes messages)
        {
            DeviceName = name;
            DeviceIndex = index;
            DeviceMessages = messages;
        }

        // Implementation details for IButtplugDeviceInfoMessage interface
        string IButtplugDeviceInfoMessage.DeviceName => DeviceName;

        uint IButtplugDeviceInfoMessage.DeviceIndex => DeviceIndex;

        DeviceMessageAttributes IButtplugDeviceInfoMessage.DeviceMessages => DeviceMessages;

        string IButtplugDeviceInfoMessage.DeviceDisplayName => DeviceDisplayName;

        uint IButtplugDeviceInfoMessage.DeviceMessageTimingGap => DeviceMessageTimingGap;
    }

    /// <summary>
    /// List of devices connected to the server.
    /// </summary>
    [ButtplugMessageMetadata("DeviceList")]
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
    /// Sent from server when a new device is discovered.
    /// </summary>
    [ButtplugMessageMetadata("DeviceAdded")]
    public class DeviceAdded : ButtplugDeviceMessage, IButtplugMessageOutgoingOnly, IButtplugDeviceInfoMessage
    {
        /// <summary>
        /// Name of device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DeviceName;

        /// <summary>
        /// Device display name, set up by the user.
        /// </summary>
        public readonly string DeviceDisplayName;

        /// <summary>
        /// Recommended amount of time between commands, in milliseconds.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly uint DeviceMessageTimingGap;

        /// <summary>
        /// Commands supported by device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly DeviceMessageAttributes DeviceMessages;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAdded"/> class.
        /// </summary>
        /// <param name="index">Device index.</param>
        /// <param name="name">Device name.</param>
        /// <param name="messages">Commands supported by device.</param>
        public DeviceAdded(uint index, string name,
            DeviceMessageAttributes messages)
            : base(ButtplugConsts.SystemMsgId, index)
        {
            DeviceName = name;
            DeviceMessages = messages;
        }

        /// <inheritdoc />
        internal DeviceAdded()
            : base(0)
        {
        }

        // Implementation details for IButtplugDeviceInfoMessage interface
        string IButtplugDeviceInfoMessage.DeviceName => DeviceName;

        uint IButtplugDeviceInfoMessage.DeviceIndex => DeviceIndex;

        DeviceMessageAttributes IButtplugDeviceInfoMessage.DeviceMessages => DeviceMessages;

        string IButtplugDeviceInfoMessage.DeviceDisplayName => DeviceDisplayName;

        uint IButtplugDeviceInfoMessage.DeviceMessageTimingGap => DeviceMessageTimingGap;
    }

    /// <summary>
    /// Sent from server when a device is disconnected.
    /// </summary>
    [ButtplugMessageMetadata("DeviceRemoved")]
    public class DeviceRemoved : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Device index.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceRemoved"/> class.
        /// </summary>
        /// <param name="index">Index of disconnected device.</param>
        public DeviceRemoved(uint index)
            : base(ButtplugConsts.SystemMsgId)
        {
            DeviceIndex = index;
        }
    }

    /// <summary>
    /// Sent to server to request a list of all connected devices.
    /// </summary>
    [ButtplugMessageMetadata("RequestDeviceList")]
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
    [ButtplugMessageMetadata("StartScanning")]
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
    [ButtplugMessageMetadata("StopScanning")]
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
    [ButtplugMessageMetadata("ScanningFinished")]
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
    [ButtplugMessageMetadata("RequestServerInfo")]
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
    [ButtplugMessageMetadata("ServerInfo")]
    public class ServerInfo : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
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
        }
    }

    /// <summary>
    /// Sent to server, at an interval specified by the server. If ping is not received in a timely
    /// manner, devices are stopped and client/server connection is severed.
    /// </summary>
    // Resharper doesn't seem to be able to deduce that though.
    // ReSharper disable once ClassNeverInstantiated.Global
    [ButtplugMessageMetadata("Ping")]
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
    /// Sent to server, generic message that can control any device that takes a single value and staticly
    /// sets an actuator to that value (speed, oscillation frequency, instanteous position, etc).
    /// </summary>
    [ButtplugMessageMetadata("ScalarCmd")]
    public class ScalarCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Container object for representing a single scalar value on a device that may have
        /// multiple independent scalar actuators.
        /// </summary>
        public class ScalarSubcommand : GenericMessageSubcommand
        {
            private double _scalarImpl;
            public readonly ActuatorType ActuatorType;

            /// <summary>
            /// Gets/sets vibration speed (0.0-1.0).
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public double Scalar
            {
                get => _scalarImpl;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentException("ScalarCmd value cannot be less than 0!");
                    }

                    if (value > 1)
                    {
                        throw new ArgumentException("ScalarCmd value cannot be greater than 1!");
                    }

                    _scalarImpl = value;
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ScalarSubcommand"/> class.
            /// </summary>
            /// <param name="index">Scalar feature index.</param>
            /// <param name="scalar">Scalar value.</param>
            public ScalarSubcommand(uint index, double scalar, ActuatorType actuatorType)
            : base(index)
            {
                Scalar = scalar;
                ActuatorType = actuatorType;
            }
        }

        /// <summary>
        /// List of vibrator speeds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<ScalarSubcommand> Scalars;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="scalars">List of per-actuator scalar commands.</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public ScalarCmd(uint deviceIndex, List<ScalarSubcommand> scalars, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            Scalars = scalars;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarCmd"/> class.
        /// </summary>
        /// <param name="scalars">List of per-actuator scalar commands.</param>
        public ScalarCmd(List<ScalarSubcommand> scalars)
            : this(uint.MaxValue, scalars)
        {
        }
    }

    /// <summary>
    /// Sent to server, generic message that can control any rotating device. This message can take
    /// multiple commands for devices with multiple rotators.
    /// </summary>
    [ButtplugMessageMetadata("RotateCmd")]
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
            foreach (var (speed, clockwise) in cmds)
            {
                cmdList.Add(new RotateSubcommand(i, speed, clockwise));
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
    [ButtplugMessageMetadata("LinearCmd")]
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
            foreach (var (duration, position) in cmds)
            {
                cmdList.Add(new VectorSubcommand(i, duration, position));
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
    [ButtplugMessageMetadata("StopDeviceCmd")]
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
    [ButtplugMessageMetadata("StopAllDevices")]
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

    /// <summary>
    /// Sent to server, generic message that can control any device that takes a single value and staticly
    /// sets an actuator to that value (speed, oscillation frequency, instanteous position, etc).
    /// </summary>
    [ButtplugMessageMetadata("SensorReadCmd")]
    public class SensorReadCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// List of vibrator speeds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint SensorIndex;

        [JsonProperty(Required = Required.Always)]
        public SensorType SensorType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="scalars">List of per-actuator scalar commands.</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public SensorReadCmd(uint deviceIndex, uint sensorIndex, SensorType sensorType, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            SensorIndex = sensorIndex;
            SensorType = sensorType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarCmd"/> class.
        /// </summary>
        /// <param name="scalars">List of per-actuator scalar commands.</param>
        public SensorReadCmd(uint sensorIndex, SensorType sensorType)
            : this(uint.MaxValue, sensorIndex, sensorType)
        {
        }
    }

    /// <summary>
    /// Sent to server, generic message that can control any device that takes a single value and staticly
    /// sets an actuator to that value (speed, oscillation frequency, instanteous position, etc).
    /// </summary>
    [ButtplugMessageMetadata("SensorReading")]
    public class SensorReading : ButtplugDeviceMessage
    {
        /// <summary>
        /// List of vibrator speeds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly uint SensorIndex;

        [JsonProperty(Required = Required.Always)]
        public readonly SensorType SensorType;

        [JsonProperty(Required = Required.Always)]
        public readonly List<int> data;
    }
}