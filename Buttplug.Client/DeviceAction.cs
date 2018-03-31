using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    /// <summary>
    /// Event wrapper for Buttplug DeviceAdded or DeviceRemoved messages. Used when the the server
    /// informs the client of a device connecting or disconnecting.
    /// </summary>
    public class DeviceEventArgs
    {
        /// <summary>
        /// Device actions are either a device added or removed.
        /// </summary>
        public enum DeviceAction
        {
            /// <summary>
            /// Signifies device was added.
            /// </summary>
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "This is part of the public API in the wild")]
            ADDED,

            /// <summary>
            /// Signifies device was removed.
            /// </summary>
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "This is part of the public API in the wild")]
            REMOVED,
        }

        /// <summary>
        /// The client representation of a Buttplug Device.
        /// </summary>
        [NotNull]
        public readonly ButtplugClientDevice Device;

        /// <summary>
        /// Device event action (added or removed).
        /// </summary>
        public readonly DeviceAction Action;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceEventArgs"/> class.
        /// </summary>
        /// <param name="aDevice">The Buttplug Device.</param>
        /// <param name="aAction">The action of the event.</param>
        public DeviceEventArgs(ButtplugClientDevice aDevice, DeviceAction aAction)
        {
            Device = aDevice;
            Action = aAction;
        }
    }
}