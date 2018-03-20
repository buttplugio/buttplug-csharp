using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    /// <summary>
    /// Event wrapper for a Buttplug DeviceAdded or DeviceRemoved message
    /// Used when the the server informs the client of a device connecting or disconnecting.
    /// </summary>
    public class DeviceEventArgs
    {
        /// <summary>
        /// Device actions are either a device added or removed
        /// </summary>
        public enum DeviceAction
        {
            /// <summary>
            /// This is a device being added
            /// </summary>
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "This is part of the public API in the wild")]
            ADDED,

            /// <summary>
            /// This is a device being removed
            /// </summary>
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "This is part of the public API in the wild")]
            REMOVED,
        }

        /// <summary>
        /// The client representation of a Buttlug Device
        /// </summary>
        [NotNull]
        public readonly ButtplugClientDevice Device;

        /// <summary>
        /// The action of this event: either a device added or removed
        /// </summary>
        public readonly DeviceAction Action;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceEventArgs"/> class.
        /// </summary>
        /// <param name="aDevice">The Buttlug Device</param>
        /// <param name="aAction">The action of this event</param>
        public DeviceEventArgs(ButtplugClientDevice aDevice, DeviceAction aAction)
        {
            Device = aDevice;
            Action = aAction;
        }
    }
}