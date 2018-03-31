using System;
using System.Collections.Generic;
using Buttplug.Core.Messages;
using Newtonsoft.Json;

namespace Buttplug.Core
{
    /// <summary>
    /// Base class for all Buttplug protocol messages.
    /// </summary>
    public abstract class ButtplugMessage
    {
        /// <summary>
        /// Message schema versions
        ///
        /// These are here for backwards compatibility support, but
        /// this also serves as a changelog of sorts.
        ///
        /// Version 0 - Originally Schema 0.1.0
        ///   First release with no backwards compatibility
        ///
        /// Version 1 - Introduction of MessageVersioning
        ///   Addition of generic commands VibrateCmd/RotateCmd/LinearCmd
        ///   Addition of message attributes
        /// </summary>
        [JsonIgnore]
        public const uint CurrentSchemaVersion = 1;

        /// <summary>
        /// The message ID
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint Id { get; set; }

        /// <summary>
        /// Gets the schema version this message was introduced in
        /// </summary>
        [JsonIgnore]
        public uint SchemaVersion
        {
            get => _schemaVersion;

            protected set => _schemaVersion = value;
        }

        /// <summary>
        /// Get the previous message type
        /// </summary>
        [JsonIgnore]
        public Type PreviousType
        {
            get => _previousType;

            protected set => _previousType = value;
        }

        // Base class starts at version 0
        [JsonIgnore]
        private uint _schemaVersion;

        // No previous version for base classes
        [JsonIgnore]
        private Type _previousType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugMessage"/> class.
        /// </summary>
        /// <param name="aId">The message ID: should be unique within a connection and non-zero, unless its a response to another message or is a server raised event.</param>
        /// <param name="aSchemaVersion">The version of the schema that the message was introduced. Required for cross version schema support.</param>
        /// <param name="aPreviousType">The Type for the previous version of the message, or null. This is used to downgrade messages when cominucating with older clients.</param>
        protected ButtplugMessage(uint aId, uint aSchemaVersion = 0, Type aPreviousType = null)
        {
            Id = aId;
            SchemaVersion = aSchemaVersion;
            PreviousType = aPreviousType;
        }
    }

    /// <summary>
    /// Interface for easy identification of Buttplug messages that should only
    /// e sent by the server, never the client.
    /// </summary>
    public interface IButtplugMessageOutgoingOnly
    {
    }

    /// <summary>
    /// Interface for messages containing Device Info, such as DeviceAdded/Removed. Allows functions
    /// to take the interface as an argument instead of having to specialize per message type.
    /// </summary>
    public interface IButtplugDeviceInfoMessage
    {
        /// <summary>
        /// The device name, which usually contains the device brand and model.
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// The device index, which uniquely identifies the device on the server.
        /// </summary>
        uint DeviceIndex { get; }

        /// <summary>
        /// The Buttplug Protocol messages supported by this device, with additional attributes.
        /// </summary>
        Dictionary<string, MessageAttributes> DeviceMessages { get; }
    }
}