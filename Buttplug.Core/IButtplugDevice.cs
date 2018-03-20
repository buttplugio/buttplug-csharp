using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    /// <summary>
    /// The interface for representations of Buttplug devices
    /// </summary>
    public interface IButtplugDevice
    {
        /// <summary>
        /// Gets the name of the device
        /// </summary>
        [NotNull]
        string Name { get; }

        /// <summary>
        /// Gets the indentifier of the device
        /// </summary>
        [NotNull]
        string Identifier { get; }

        /// <summary>
        /// Gets or sets the index of the device
        /// </summary>
        uint Index { get; set; }

        /// <summary>
        /// Gets a value indicating whether the device is connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Event handler for signalling the device has been removed
        /// </summary>
        [CanBeNull]
        event EventHandler DeviceRemoved;

        /// <summary>
        /// Event handler for signalling when the device has data to share
        /// </summary>
        [CanBeNull]
        event EventHandler<MessageReceivedEventArgs> MessageEmitted;

        /// <summary>
        /// Gets allowed message types for this device
        /// </summary>
        /// <returns>An enumerable of messahe types</returns>
        [NotNull]
        IEnumerable<Type> GetAllowedMessageTypes();

        /// <summary>
        /// Interprets a Buttplug device message by ensuring it is supported
        /// by this device, then invoking the handler method and returing the
        /// resulting Buttplug message.
        /// </summary>
        /// <param name="aMsg">The device message to handle</param>
        /// <returns>The message response</returns>
        [NotNull]
        Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg);

        /// <summary>
        /// Initiaizes a device (perform any one-tme-setup stuff for example)
        /// </summary>
        /// <returns>A message resonse to the initialisation</returns>
        [NotNull]
        Task<ButtplugMessage> Initialize();

        /// <summary>
        /// Disconnects the device
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Invokes the EmitMessage event handler.
        /// Required to allow events to be raised for this device from the lower levels.
        /// </summary>
        /// <param name="aMsg">The message to emit from the device</param>
        [NotNull]
        MessageAttributes GetMessageAttrs(Type aMsg);
    }
}
