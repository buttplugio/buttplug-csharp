// <copyright file="ButtplugMessage.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

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


        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugMessage"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        protected ButtplugMessage(uint id)
        {
            Id = id;
        }

        private static Dictionary<Type, ButtplugMessageMetadata> _metadataCache = new Dictionary<Type, ButtplugMessageMetadata>();

        /// <summary>
        /// Gets a certain ButtplugMessageMetadata attributes for a ButtplugMessage.
        /// </summary>
        /// <typeparam name="T">Return type expected. </typeparam>
        /// <param name="msgType">Type to get attribute from.</param>
        /// <param name="func">Lambda that returns the required attribute.</param>
        /// <returns>Attribute requested.</returns>
        /// <exception cref="ArgumentNullException">Thrown if msgType or func is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if msgType does not have ButtplugMessageMetadata Attributes.</exception>
        private static T GetMessageAttribute<T>(Type msgType, Func<ButtplugMessageMetadata, T> func)
        {
            ButtplugUtils.ArgumentNotNull(msgType, nameof(msgType));
            ButtplugUtils.ArgumentNotNull(func, nameof(func));
            if (!msgType.IsSubclassOf(typeof(ButtplugMessage)))
            {
                throw new ArgumentException($"Argument {msgType.Name} must be a subclass of ButtplugMessage");
            }

            if (_metadataCache.ContainsKey(msgType))
            {
                return func(_metadataCache[msgType]);
            }

            // Message creation is extremely hot path, and these are queried a lot. All of
            // these loops for lookups may get slow.
            var attrs = Attribute.GetCustomAttributes(msgType);

            // Displaying output.
            foreach (var attr in attrs)
            {
                if (attr is ButtplugMessageMetadata metadata)
                {
                    _metadataCache[msgType] = metadata;
                    return func(metadata);
                }
            }

            throw new ArgumentException($"Type {msgType} does not have ButtplugMessageMetadata Attributes");
        }

        /// <summary>
        /// Returns name of a ButtplugMessage with ButtplugMessageMetadata attributes.
        /// </summary>
        /// <param name="msgType">Type to get data from.</param>
        /// <returns>Message name of ButtplugMessage class type.</returns>
        public static string GetName(Type msgType)
        {
            return GetMessageAttribute(msgType, (md) => md.Name);
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
        DeviceMessageAttributes DeviceMessages { get; }
    }
}