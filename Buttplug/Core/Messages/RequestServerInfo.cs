using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Sent to server to set up client information, including client name and schema version.
    /// Denotes the beginning of a connection handshake.
    /// </summary>
    [ButtplugMessageMetadata("RequestServerInfo")]
    public class RequestServerInfo : ButtplugMessage
    {
        /// <summary>
        /// Client name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ClientName;

        /// <summary>
        /// Client message schema version.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint ProtocolVersionMajor = ButtplugConsts.ProtocolVersionMajor;

        [JsonProperty(Required = Required.Always)]
        public uint ProtocolVersionMinor = ButtplugConsts.ProtocolVersionMinor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestServerInfo"/> class.
        /// </summary>
        /// <param name="clientName">Client name.</param>
        /// <param name="id">Message id.</param>
        public RequestServerInfo(string clientName, uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
            ClientName = clientName;
        }
    }
}