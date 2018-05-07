using System;
using System.Collections.Generic;
using Buttplug.Core.Messages;
using Newtonsoft.Json;

namespace Buttplug.Core
{
    /// <summary>
    /// Base class for Buttplug protocol messages.
    /// </summary>
    public abstract class ButtplugMessage
    {
        /// <summary>
        /// Current message schema version. History of versions can be seen at https://github.com/metafetish/buttplug-schema.
        /// </summary>
        [JsonIgnore]
        public const uint CurrentSchemaVersion = 1;

        /// <summary>
        /// Message ID.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint Id { get; set; }

        /// <summary>
        /// Schema version message was introduced in.
        /// </summary>
        [JsonIgnore]
        public uint SchemaVersion
        {
            get => _schemaVersion;

            protected set => _schemaVersion = value;
        }

        /// <summary>
        /// Previous message type, if the message has changed between schema versions.
        /// </summary>
        [JsonIgnore]
        public Type PreviousType
        {
            get => _previousType;

            protected set => _previousType = value;
        }

        // Storage for the schema version.
        [JsonIgnore]
        private uint _schemaVersion;

        // Storage of previous type. Will be null for base classes and messages without previous types.
        [JsonIgnore]
        private Type _previousType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugMessage"/> class.
        /// </summary>
        /// <param name="aId">Message ID</param>
        /// <param name="aSchemaVersion">Schema version where message was introduced</param>
        /// <param name="aPreviousType">Type for previous version of message, if one exists.</param>
        protected ButtplugMessage(uint aId, uint aSchemaVersion = 0, Type aPreviousType = null)
        {
            Id = aId;
            SchemaVersion = aSchemaVersion;
            PreviousType = aPreviousType;
        }
    }

    /// <summary>
    /// Interface for messages only sent from server to client.
    /// </summary>
    public interface IButtplugMessageOutgoingOnly
    {
    }

    /// <summary>
    /// Interface for messages containing Device Info, such as DeviceAdded/Removed.
    /// </summary>
    public interface IButtplugDeviceInfoMessage
    {
        /// <summary>
        /// Device name.
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Device index, as assigned by a Buttplug server.
        /// </summary>
        uint DeviceIndex { get; }

        /// <summary>
        /// Buttplug messages supported by this device, with additional attributes.
        /// </summary>
        Dictionary<string, MessageAttributes> DeviceMessages { get; }
    }
}