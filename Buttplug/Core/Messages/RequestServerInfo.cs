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
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public uint MessageVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestServerInfo"/> class.
        /// </summary>
        /// <param name="clientName">Client name.</param>
        /// <param name="id">Message Id.</param>
        /// <param name="schemversion">Message schema version.</param>
        public RequestServerInfo(string clientName, uint id = ButtplugConsts.DefaultMsgId, uint schemversion = ButtplugConsts.CurrentSpecVersion)
            : base(id)
        {
            ClientName = clientName;
            MessageVersion = schemversion;
        }
    }
}