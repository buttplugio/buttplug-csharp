using JetBrains.Annotations;

namespace Buttplug.Client
{
    /// <summary>
    /// Event wrapper for Buttplug DeviceAdded or DeviceRemoved messages. Used when the the server
    /// informs the client of a device connecting or disconnecting.
    /// </summary>
    public class DeviceRemovedEventArgs
    {
        /// <summary>
        /// The client representation of a Buttplug Device.
        /// </summary>
        [NotNull]
        public readonly ButtplugClientDevice Device;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceRemovedEventArgs"/> class.
        /// </summary>
        /// <param name="aDevice">Device being removed</param>
        public DeviceRemovedEventArgs(ButtplugClientDevice aDevice)
        {
            Device = aDevice;
        }
    }
}