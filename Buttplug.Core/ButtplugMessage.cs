using System;
using Newtonsoft.Json;

namespace Buttplug.Core
{
    public abstract class ButtplugMessage
    {
        /*
         * Message schema versions
         *
         * These are here for backwards compatibility support, but
         * this also serves as a changelog of sorts.
         *
         * Version 0 - Originally Schema 0.1.0
         *   First release with no backwards compatibility
         *
         * Version 1 - Introduction of MessageVersioning
         *   Addition of generic commands VibrateCmd/RotateCmd/LinearCmd
         *   Addition of message attributes
         */
        [JsonIgnore]
        public const uint CurrentSchemaVersion = 1;

        [JsonProperty(Required = Required.Always)]
        public uint Id { get; set; }

        [JsonIgnore]
        public uint SchemaVersion
        {
            get => _schemaVersion;

            protected set => _schemaVersion = value;
        }

        [JsonIgnore]
        public Type PreviousType
        {
            get => _previousType;

            protected set => _previousType = value;
        }

        // Base class starts at version 0
        [JsonIgnore]
        private uint _schemaVersion = 0;

        // No previous version for base classes
        [JsonIgnore]
        private Type _previousType = null;

        protected ButtplugMessage(uint aId, uint aSchemaVersion = 0, Type aPreviousType = null)
        {
            Id = aId;
            SchemaVersion = aSchemaVersion;
            PreviousType = aPreviousType;
        }
    }

    public interface IButtplugMessageOutgoingOnly
    {
    }
}