// <copyright file="Messages.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

// Namespace containing all Buttplug messages, as specified by the Buttplug Message Spec at
// https://docs.buttplug.io/spec. For consistency sake, all message descriptions are stated in
// relation to the server, i.e. message are sent "(from client) to server" or "(to client) from server".
namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Output actuator types supported by the protocol.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OutputType
    {
        [EnumMember(Value = "Unknown")]
        Unknown,
        [EnumMember(Value = "Vibrate")]
        Vibrate,
        [EnumMember(Value = "Oscillate")]
        Oscillate,
        [EnumMember(Value = "Rotate")]
        Rotate,
        [EnumMember(Value = "Position")]
        Position,
        [EnumMember(Value = "HwPositionWithDuration")]
        HwPositionWithDuration,
        [EnumMember(Value = "Led")]
        Led,
        [EnumMember(Value = "Temperature")]
        Temperature,
        [EnumMember(Value = "Constrict")]
        Constrict,
        [EnumMember(Value = "Spray")]
        Spray
    }

    /// <summary>
    /// Input sensor types supported by the protocol.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum InputType
    {
        [EnumMember(Value = "Unknown")]
        Unknown,
        [EnumMember(Value = "Battery")]
        Battery,
        [EnumMember(Value = "RSSI")]
        RSSI,
        [EnumMember(Value = "Button")]
        Button,
        [EnumMember(Value = "Pressure")]
        Pressure,
        [EnumMember(Value = "Depth")]
        Depth,
        [EnumMember(Value = "Position")]
        Position
    }

    /// <summary>
    /// Command types for input sensors.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum InputCommandType
    {
        [EnumMember(Value = "Read")]
        Read,
        [EnumMember(Value = "Subscribe")]
        Subscribe,
        [EnumMember(Value = "Unsubscribe")]
        Unsubscribe
    }

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

    #region Device Feature Definitions

    /// <summary>
    /// Output feature attributes for a device feature.
    /// </summary>
    public class DeviceFeatureOutput
    {
        /// <summary>
        /// The inclusive range of valid values for this output [min, max].
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int[] Value;

        /// <summary>
        /// For PositionWithDuration, the inclusive range of valid duration values [min, max].
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public int[] Duration;
    }

    /// <summary>
    /// Input feature attributes for a device feature.
    /// </summary>
    public class DeviceFeatureInput
    {
        /// <summary>
        /// Array of value ranges, each being [min, max].
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int[][] Value;

        /// <summary>
        /// List of supported commands (Read, Subscribe, Unsubscribe).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public InputCommandType[] Command;
    }

    /// <summary>
    /// Represents a single feature of a device in V4 protocol.
    /// </summary>
    public class DeviceFeature
    {
        /// <summary>
        /// Human-readable description of this feature.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string FeatureDescriptor;

        /// <summary>
        /// Index of this feature within the device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint FeatureIndex;

        /// <summary>
        /// Output capabilities of this feature, keyed by OutputType name.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, DeviceFeatureOutput> Output;

        /// <summary>
        /// Input capabilities of this feature, keyed by InputType name.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, DeviceFeatureInput> Input;

        /// <summary>
        /// Check if this feature has a specific output type.
        /// </summary>
        public bool HasOutput(OutputType type)
        {
            return Output != null && Output.ContainsKey(type.ToString());
        }

        /// <summary>
        /// Check if this feature has a specific input type.
        /// </summary>
        public bool HasInput(InputType type)
        {
            return Input != null && Input.ContainsKey(type.ToString());
        }

        /// <summary>
        /// Get the output attributes for a specific output type.
        /// </summary>
        public DeviceFeatureOutput GetOutput(OutputType type)
        {
            if (Output == null || !Output.ContainsKey(type.ToString()))
            {
                return null;
            }
            return Output[type.ToString()];
        }

        /// <summary>
        /// Get the input attributes for a specific input type.
        /// </summary>
        public DeviceFeatureInput GetInput(InputType type)
        {
            if (Input == null || !Input.ContainsKey(type.ToString()))
            {
                return null;
            }
            return Input[type.ToString()];
        }
    }

    #endregion

    #region Device Info

    /// <summary>
    /// Container class for describing a device in V4 protocol.
    /// </summary>
    public class DeviceInfo : IButtplugDeviceInfoMessage
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
        /// Device display name, set up by the user.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string DeviceDisplayName;

        /// <summary>
        /// Recommended amount of time between commands, in milliseconds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceMessageTimingGap;

        /// <summary>
        /// Device features, keyed by feature index as string.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, DeviceFeature> DeviceFeatures;

        /// <summary>
        /// Gets a feature by its index.
        /// </summary>
        public DeviceFeature GetFeature(uint index)
        {
            var key = index.ToString();
            if (DeviceFeatures != null && DeviceFeatures.ContainsKey(key))
            {
                return DeviceFeatures[key];
            }
            return null;
        }

        /// <summary>
        /// Gets all features as an enumerable.
        /// </summary>
        public IEnumerable<DeviceFeature> GetAllFeatures()
        {
            if (DeviceFeatures == null)
            {
                return Enumerable.Empty<DeviceFeature>();
            }
            return DeviceFeatures.Values;
        }

        /// <summary>
        /// Gets all features that have a specific output type.
        /// </summary>
        public IEnumerable<DeviceFeature> GetFeaturesWithOutput(OutputType type)
        {
            return GetAllFeatures().Where(f => f.HasOutput(type));
        }

        /// <summary>
        /// Gets all features that have a specific input type.
        /// </summary>
        public IEnumerable<DeviceFeature> GetFeaturesWithInput(InputType type)
        {
            return GetAllFeatures().Where(f => f.HasInput(type));
        }

        // IButtplugDeviceInfoMessage implementation
        string IButtplugDeviceInfoMessage.DeviceName => DeviceName;
        uint IButtplugDeviceInfoMessage.DeviceIndex => DeviceIndex;
        string IButtplugDeviceInfoMessage.DeviceDisplayName => DeviceDisplayName;
        uint IButtplugDeviceInfoMessage.DeviceMessageTimingGap => DeviceMessageTimingGap;
        Dictionary<string, DeviceFeature> IButtplugDeviceInfoMessage.DeviceFeatures => DeviceFeatures;
    }

    #endregion

    #region Device List

    /// <summary>
    /// Custom JSON converter for DeviceList that handles the dictionary keyed by device index strings.
    /// </summary>
    public class DeviceListConverter : JsonConverter<DeviceList>
    {
        public override DeviceList ReadJson(JsonReader reader, Type objectType, DeviceList existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var deviceList = new DeviceList();

            if (obj["Id"] != null)
            {
                deviceList.Id = obj["Id"].Value<uint>();
            }

            deviceList.Devices = new Dictionary<string, DeviceInfo>();
            if (obj["Devices"] != null)
            {
                var devicesObj = obj["Devices"] as JObject;
                if (devicesObj != null)
                {
                    foreach (var prop in devicesObj.Properties())
                    {
                        var deviceInfo = prop.Value.ToObject<DeviceInfo>(serializer);
                        deviceList.Devices[prop.Name] = deviceInfo;
                    }
                }
            }

            return deviceList;
        }

        public override void WriteJson(JsonWriter writer, DeviceList value, JsonSerializer serializer)
        {
            var obj = new JObject();
            obj["Id"] = value.Id;

            var devicesObj = new JObject();
            if (value.Devices != null)
            {
                foreach (var kvp in value.Devices)
                {
                    devicesObj[kvp.Key] = JObject.FromObject(kvp.Value, serializer);
                }
            }
            obj["Devices"] = devicesObj;

            obj.WriteTo(writer);
        }
    }

    /// <summary>
    /// List of devices connected to the server.
    /// </summary>
    [ButtplugMessageMetadata("DeviceList")]
    [JsonConverter(typeof(DeviceListConverter))]
    public class DeviceList : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Devices currently connected, keyed by device index as string.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, DeviceInfo> Devices;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceList"/> class.
        /// </summary>
        /// <param name="devices">Dictionary of devices keyed by index.</param>
        /// <param name="id">Message ID.</param>
        public DeviceList(Dictionary<string, DeviceInfo> devices, uint id)
            : base(id)
        {
            Devices = devices ?? new Dictionary<string, DeviceInfo>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceList"/> class.
        /// </summary>
        internal DeviceList()
            : base(0)
        {
            Devices = new Dictionary<string, DeviceInfo>();
        }

        /// <summary>
        /// Gets all devices as an enumerable.
        /// </summary>
        public IEnumerable<DeviceInfo> GetAllDevices()
        {
            return Devices?.Values ?? Enumerable.Empty<DeviceInfo>();
        }

        /// <summary>
        /// Gets a device by its index.
        /// </summary>
        public DeviceInfo GetDevice(uint index)
        {
            var key = index.ToString();
            if (Devices != null && Devices.ContainsKey(key))
            {
                return Devices[key];
            }
            return null;
        }
    }

    #endregion

    #region Scanning and Device Discovery

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

    #endregion

    #region Server Handshake

    /// <summary>
    /// Sent to server to set up client information, including client name and protocol version.
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
        /// Client protocol major version.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint ProtocolVersionMajor;

        /// <summary>
        /// Client protocol minor version.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint ProtocolVersionMinor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestServerInfo"/> class.
        /// </summary>
        /// <param name="clientName">Client name.</param>
        /// <param name="id">Message Id.</param>
        /// <param name="protocolVersionMajor">Protocol major version.</param>
        /// <param name="protocolVersionMinor">Protocol minor version.</param>
        public RequestServerInfo(
            string clientName,
            uint id = ButtplugConsts.DefaultMsgId,
            uint protocolVersionMajor = ButtplugConsts.ProtocolVersionMajor,
            uint protocolVersionMinor = ButtplugConsts.ProtocolVersionMinor)
            : base(id)
        {
            ClientName = clientName;
            ProtocolVersionMajor = protocolVersionMajor;
            ProtocolVersionMinor = protocolVersionMinor;
        }
    }

    /// <summary>
    /// Sent from server, in response to <see cref="RequestServerInfo"/>. Contains server name,
    /// protocol version, ping information, etc...
    /// </summary>
    [ButtplugMessageMetadata("ServerInfo")]
    public class ServerInfo : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// The major protocol version of the server.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint ProtocolVersionMajor;

        /// <summary>
        /// The minor protocol version of the server.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint ProtocolVersionMinor;

        /// <summary>
        /// Expected ping time (in milliseconds). 0 means no ping required.
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
        /// <param name="protocolVersionMajor">Server protocol major version.</param>
        /// <param name="protocolVersionMinor">Server protocol minor version.</param>
        /// <param name="maxPingTime">Ping timeout.</param>
        /// <param name="id">Message ID.</param>
        public ServerInfo(
            string serverName,
            uint protocolVersionMajor,
            uint protocolVersionMinor,
            uint maxPingTime,
            uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
            ServerName = serverName;
            ProtocolVersionMajor = protocolVersionMajor;
            ProtocolVersionMinor = protocolVersionMinor;
            MaxPingTime = maxPingTime;
        }

        /// <summary>
        /// Combined message version for backwards compatibility checks.
        /// </summary>
        [JsonIgnore]
        public uint MessageVersion => ProtocolVersionMajor;
    }

    #endregion

    #region Connection Management

    /// <summary>
    /// Sent to server, at an interval specified by the server. If ping is not received in a timely
    /// manner, devices are stopped and client/server connection is severed.
    /// </summary>
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

    #endregion

    #region Device Control - Stop Commands

    /// <summary>
    /// Sent to server, stops actions of a specific device.
    /// </summary>
    [ButtplugMessageMetadata("StopDeviceCmd")]
    public class StopDeviceCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// If true, stop all input operations (subscriptions).
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? Inputs;

        /// <summary>
        /// If true, stop all output operations.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? Outputs;

        /// <summary>
        /// Initializes a new instance of the <see cref="StopDeviceCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="id">Message ID.</param>
        /// <param name="inputs">Stop inputs.</param>
        /// <param name="outputs">Stop outputs.</param>
        public StopDeviceCmd(
            uint deviceIndex = uint.MaxValue,
            uint id = ButtplugConsts.DefaultMsgId,
            bool? inputs = null,
            bool? outputs = null)
            : base(id, deviceIndex)
        {
            Inputs = inputs;
            Outputs = outputs;
        }
    }

    /// <summary>
    /// Sent to server, stops actions of all currently connected devices.
    /// </summary>
    [ButtplugMessageMetadata("StopAllDevices")]
    public class StopAllDevices : ButtplugMessage
    {
        /// <summary>
        /// If true, stop all input operations (subscriptions).
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? Inputs;

        /// <summary>
        /// If true, stop all output operations.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? Outputs;

        /// <summary>
        /// Initializes a new instance of the <see cref="StopAllDevices"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        /// <param name="inputs">Stop inputs.</param>
        /// <param name="outputs">Stop outputs.</param>
        public StopAllDevices(
            uint id = ButtplugConsts.DefaultMsgId,
            bool? inputs = null,
            bool? outputs = null)
            : base(id)
        {
            Inputs = inputs;
            Outputs = outputs;
        }
    }

    #endregion

    #region Device Control - Output Commands

    /// <summary>
    /// Output command value for non-duration outputs.
    /// </summary>
    public class OutputCommandValue
    {
        /// <summary>
        /// The value to set.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double Value;

        public OutputCommandValue()
        {
        }

        public OutputCommandValue(double value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Output command value for PositionWithDuration outputs.
    /// </summary>
    public class OutputCommandValueWithDuration
    {
        /// <summary>
        /// The position value to set.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double Value;

        /// <summary>
        /// The duration in milliseconds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint Duration;

        public OutputCommandValueWithDuration()
        {
        }

        public OutputCommandValueWithDuration(double value, uint duration)
        {
            Value = value;
            Duration = duration;
        }
    }

    /// <summary>
    /// Custom JSON converter for OutputCmd to handle the dynamic Command property.
    /// </summary>
    public class OutputCmdConverter : JsonConverter<OutputCmd>
    {
        public override OutputCmd ReadJson(JsonReader reader, Type objectType, OutputCmd existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var cmd = new OutputCmd();

            if (obj["Id"] != null)
            {
                cmd.Id = obj["Id"].Value<uint>();
            }
            if (obj["DeviceIndex"] != null)
            {
                cmd.DeviceIndex = obj["DeviceIndex"].Value<uint>();
            }
            if (obj["FeatureIndex"] != null)
            {
                cmd.FeatureIndex = obj["FeatureIndex"].Value<uint>();
            }

            cmd.Command = new Dictionary<string, object>();
            if (obj["Command"] != null)
            {
                var commandObj = obj["Command"] as JObject;
                if (commandObj != null)
                {
                    foreach (var prop in commandObj.Properties())
                    {
                        if (prop.Name == "HwPositionWithDuration")
                        {
                            cmd.Command[prop.Name] = prop.Value.ToObject<OutputCommandValueWithDuration>(serializer);
                        }
                        else
                        {
                            cmd.Command[prop.Name] = prop.Value.ToObject<OutputCommandValue>(serializer);
                        }
                    }
                }
            }

            return cmd;
        }

        public override void WriteJson(JsonWriter writer, OutputCmd value, JsonSerializer serializer)
        {
            var obj = new JObject();
            obj["Id"] = value.Id;
            obj["DeviceIndex"] = value.DeviceIndex;
            obj["FeatureIndex"] = value.FeatureIndex;

            var commandObj = new JObject();
            if (value.Command != null)
            {
                foreach (var kvp in value.Command)
                {
                    commandObj[kvp.Key] = JObject.FromObject(kvp.Value, serializer);
                }
            }
            obj["Command"] = commandObj;

            obj.WriteTo(writer);
        }
    }

    /// <summary>
    /// Sent to server, generic message that controls device outputs (vibration, rotation, position, etc.).
    /// Replaces ScalarCmd, RotateCmd, and LinearCmd from V3.
    /// </summary>
    [ButtplugMessageMetadata("OutputCmd")]
    [JsonConverter(typeof(OutputCmdConverter))]
    public class OutputCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// The feature index to control.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint FeatureIndex;

        /// <summary>
        /// The command to send, keyed by output type name.
        /// Values are either OutputCommandValue or OutputCommandValueWithDuration.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, object> Command;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputCmd"/> class.
        /// </summary>
        public OutputCmd()
            : base(ButtplugConsts.DefaultMsgId, uint.MaxValue)
        {
            Command = new Dictionary<string, object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="featureIndex">Feature index.</param>
        /// <param name="id">Message ID.</param>
        public OutputCmd(uint deviceIndex, uint featureIndex, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            FeatureIndex = featureIndex;
            Command = new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates an OutputCmd for a simple value output (Vibrate, Oscillate, etc.).
        /// </summary>
        public static OutputCmd Create(uint deviceIndex, uint featureIndex, OutputType outputType, double value, uint id = ButtplugConsts.DefaultMsgId)
        {
            var cmd = new OutputCmd(deviceIndex, featureIndex, id);
            cmd.Command[outputType.ToString()] = new OutputCommandValue(value);
            return cmd;
        }

        /// <summary>
        /// Creates an OutputCmd for HwPositionWithDuration output.
        /// </summary>
        public static OutputCmd CreatePositionWithDuration(uint deviceIndex, uint featureIndex, double position, uint duration, uint id = ButtplugConsts.DefaultMsgId)
        {
            var cmd = new OutputCmd(deviceIndex, featureIndex, id);
            cmd.Command[OutputType.HwPositionWithDuration.ToString()] = new OutputCommandValueWithDuration(position, duration);
            return cmd;
        }

        /// <summary>
        /// Sets a simple value command.
        /// </summary>
        public OutputCmd WithValue(OutputType outputType, double value)
        {
            Command[outputType.ToString()] = new OutputCommandValue(value);
            return this;
        }

        /// <summary>
        /// Sets a position with duration command.
        /// </summary>
        public OutputCmd WithPositionAndDuration(double position, uint duration)
        {
            Command[OutputType.HwPositionWithDuration.ToString()] = new OutputCommandValueWithDuration(position, duration);
            return this;
        }
    }

    #endregion

    #region Device Control - Input Commands

    /// <summary>
    /// Sent to server, requests a sensor reading or manages sensor subscriptions.
    /// Replaces SensorReadCmd and SensorSubscribeCmd from V3.
    /// </summary>
    [ButtplugMessageMetadata("InputCmd")]
    public class InputCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// The feature index to read from.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint FeatureIndex;

        /// <summary>
        /// The input type to read (Battery, RSSI, Button, Pressure, etc.).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public InputType Type;

        /// <summary>
        /// The command type (Read, Subscribe, Unsubscribe).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public InputCommandType Command;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputCmd"/> class.
        /// </summary>
        public InputCmd()
            : base(ButtplugConsts.DefaultMsgId, uint.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="featureIndex">Feature index.</param>
        /// <param name="type">Input type.</param>
        /// <param name="command">Command type.</param>
        /// <param name="id">Message ID.</param>
        public InputCmd(
            uint deviceIndex,
            uint featureIndex,
            InputType type,
            InputCommandType command,
            uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            FeatureIndex = featureIndex;
            Type = type;
            Command = command;
        }

        /// <summary>
        /// Creates an InputCmd to read a sensor value.
        /// </summary>
        public static InputCmd Read(uint deviceIndex, uint featureIndex, InputType type, uint id = ButtplugConsts.DefaultMsgId)
        {
            return new InputCmd(deviceIndex, featureIndex, type, InputCommandType.Read, id);
        }

        /// <summary>
        /// Creates an InputCmd to subscribe to sensor updates.
        /// </summary>
        public static InputCmd Subscribe(uint deviceIndex, uint featureIndex, InputType type, uint id = ButtplugConsts.DefaultMsgId)
        {
            return new InputCmd(deviceIndex, featureIndex, type, InputCommandType.Subscribe, id);
        }

        /// <summary>
        /// Creates an InputCmd to unsubscribe from sensor updates.
        /// </summary>
        public static InputCmd Unsubscribe(uint deviceIndex, uint featureIndex, InputType type, uint id = ButtplugConsts.DefaultMsgId)
        {
            return new InputCmd(deviceIndex, featureIndex, type, InputCommandType.Unsubscribe, id);
        }
    }

    /// <summary>
    /// Value container for sensor readings.
    /// </summary>
    public class InputReadingValue
    {
        /// <summary>
        /// The sensor value.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double Value;

        public InputReadingValue()
        {
        }

        public InputReadingValue(double value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Custom JSON converter for InputReading to handle the dynamic Reading property.
    /// </summary>
    public class InputReadingConverter : JsonConverter<InputReading>
    {
        public override InputReading ReadJson(JsonReader reader, Type objectType, InputReading existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var reading = new InputReading();

            if (obj["Id"] != null)
            {
                reading.Id = obj["Id"].Value<uint>();
            }
            if (obj["DeviceIndex"] != null)
            {
                reading.DeviceIndex = obj["DeviceIndex"].Value<uint>();
            }
            if (obj["FeatureIndex"] != null)
            {
                reading.FeatureIndex = obj["FeatureIndex"].Value<uint>();
            }

            reading.Reading = new Dictionary<string, InputReadingValue>();
            if (obj["Reading"] != null)
            {
                var readingObj = obj["Reading"] as JObject;
                if (readingObj != null)
                {
                    foreach (var prop in readingObj.Properties())
                    {
                        reading.Reading[prop.Name] = prop.Value.ToObject<InputReadingValue>(serializer);
                    }
                }
            }

            return reading;
        }

        public override void WriteJson(JsonWriter writer, InputReading value, JsonSerializer serializer)
        {
            var obj = new JObject();
            obj["Id"] = value.Id;
            obj["DeviceIndex"] = value.DeviceIndex;
            obj["FeatureIndex"] = value.FeatureIndex;

            var readingObj = new JObject();
            if (value.Reading != null)
            {
                foreach (var kvp in value.Reading)
                {
                    readingObj[kvp.Key] = JObject.FromObject(kvp.Value, serializer);
                }
            }
            obj["Reading"] = readingObj;

            obj.WriteTo(writer);
        }
    }

    /// <summary>
    /// Sent from server, contains a sensor reading from a device.
    /// Replaces SensorReading from V3.
    /// </summary>
    [ButtplugMessageMetadata("InputReading")]
    [JsonConverter(typeof(InputReadingConverter))]
    public class InputReading : ButtplugDeviceMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// The feature index the reading is from.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint FeatureIndex;

        /// <summary>
        /// The reading values, keyed by input type name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, InputReadingValue> Reading;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputReading"/> class.
        /// </summary>
        public InputReading()
            : base(ButtplugConsts.SystemMsgId, uint.MaxValue)
        {
            Reading = new Dictionary<string, InputReadingValue>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputReading"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="featureIndex">Feature index.</param>
        /// <param name="id">Message ID.</param>
        public InputReading(uint deviceIndex, uint featureIndex, uint id = ButtplugConsts.SystemMsgId)
            : base(id, deviceIndex)
        {
            FeatureIndex = featureIndex;
            Reading = new Dictionary<string, InputReadingValue>();
        }

        /// <summary>
        /// Gets a reading value for a specific input type.
        /// </summary>
        public double? GetValue(InputType type)
        {
            var key = type.ToString();
            if (Reading != null && Reading.ContainsKey(key))
            {
                return Reading[key].Value;
            }
            return null;
        }

        /// <summary>
        /// Gets the battery level as a percentage (0.0 to 1.0).
        /// </summary>
        public double? BatteryLevel => GetValue(InputType.Battery);

        /// <summary>
        /// Gets the RSSI level.
        /// </summary>
        public double? RSSILevel => GetValue(InputType.RSSI);
    }

    #endregion
}
