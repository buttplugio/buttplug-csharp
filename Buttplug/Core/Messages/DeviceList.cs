using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// List of devices connected to the server.
    /// </summary>
    [ButtplugMessageMetadata("DeviceList")]
    public class DeviceList : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// List of devices currently connected.
        /// </summary>
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public readonly DeviceMessageInfo[] Devices = new DeviceMessageInfo[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceList"/> class.
        /// </summary>
        /// <param name="deviceList">List of devices currently connected.</param>
        /// <param name="id">Message ID.</param>
        public DeviceList(DeviceMessageInfo[] deviceList, uint id)
            : base(id)
        {
            Devices = deviceList;
        }

        /// <inheritdoc />
        internal DeviceList()
            : base(0)
        {
        }
    }
}