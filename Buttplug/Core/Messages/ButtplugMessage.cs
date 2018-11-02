using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Base class for Buttplug protocol messages.
    /// </summary>
    public abstract class ButtplugMessage
    {
        /// <summary>
        /// Message ID.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint Id { get; set; }

        [JsonIgnore]
        public string Name => GetName(GetType());

        [JsonIgnore]
        public uint SpecVersion => GetSpecVersion(GetType());

        [JsonIgnore]
        public Type PreviousType => GetPreviousType(GetType());

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugMessage"/> class.
        /// </summary>
        /// <param name="aId">Message ID</param>
        protected ButtplugMessage(uint aId)
        {
            Id = aId;
        }

        private static Dictionary<Type, ButtplugMessageMetadata> _metadataCache = new Dictionary<Type, ButtplugMessageMetadata>();

        /// <summary>
        /// Gets a certain ButtplugMessageMetadata attributes for a ButtplugMessage
        /// </summary>
        /// <typeparam name="T">Return type expected </typeparam>
        /// <param name="aMsgType">Type to get attribute from</param>
        /// <param name="aFunc">Lambda that returns the required attribute.</param>
        /// <returns>Attribute requested.</returns>
        /// <exception cref="ArgumentNullException">Thrown if aMsgType or aFunc is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if aMsgType does not have ButtplugMessageMetadata Attributes.</exception>
        private static T GetMessageAttribute<T>(Type aMsgType, Func<ButtplugMessageMetadata, T> aFunc)
        {
            ButtplugUtils.ArgumentNotNull(aMsgType, nameof(aMsgType));
            ButtplugUtils.ArgumentNotNull(aFunc, nameof(aFunc));
            if (!aMsgType.IsSubclassOf(typeof(ButtplugMessage)))
            {
                throw new ArgumentException($"Argument {aMsgType.Name} must be a subclass of ButtplugMessage");
            }

            if (_metadataCache.ContainsKey(aMsgType))
            {
                return aFunc(_metadataCache[aMsgType]);
            }

            // Message creation is extremely hot path, and these are queried a lot. All of
            // these loops for lookups may get slow.
            var attrs = Attribute.GetCustomAttributes(aMsgType);

            // Displaying output.  
            foreach (var attr in attrs)
            {
                if (attr is ButtplugMessageMetadata metadata)
                {
                    _metadataCache[aMsgType] = metadata;
                    return aFunc(metadata);
                }
            }

            throw new ArgumentException($"Type {aMsgType} does not have ButtplugMessageMetadata Attributes");
        }

        /// <summary>
        /// Returns name of a ButtplugMessage with ButtplugMessageMetadata attributes.
        /// </summary>
        /// <param name="aMsgType">Type to get data from.</param>
        /// <returns>Message name of ButtplugMessage class type.</returns>
        public static string GetName(Type aMsgType)
        {
            return GetMessageAttribute(aMsgType, (aMd) => aMd.Name);
        }

        /// <summary>
        /// Returns spec version of a ButtplugMessage with ButtplugMessageMetadata attributes.
        /// </summary>
        /// <param name="aMsgType">Type to get data from.</param>
        /// <returns>Spec version of ButtplugMessage class type.</returns>
        public static uint GetSpecVersion(Type aMsgType)
        {
            return GetMessageAttribute(aMsgType, (aMd) => aMd.Version);
        }

        /// <summary>
        /// Returns previous type of a ButtplugMessage with ButtplugMessageMetadata attributes, if it exists.
        /// </summary>
        /// <param name="aMsgType">Type to get data from.</param>
        /// <returns>Previous message type version of ButtplugMessage class type, or null if no previous message type exists.</returns>
        public static Type GetPreviousType(Type aMsgType)
        {
            return GetMessageAttribute(aMsgType, (aMd) => aMd.PreviousType);
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