using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Sent from server, in response to <see cref="RequestServerInfo"/>. Contains server name,
    /// message version, ping information, etc...
    /// </summary>
    [ButtplugMessageMetadata("ServerInfo")]
    public class ServerInfo : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// The schema version of the server. Must be greater or equal to version client reported in <see cref="RequestServerInfo"/>.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint MessageVersion;

        /// <summary>
        /// Expected ping time (in milliseconds).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint MaxPingTime;

        /// <summary>
        /// Server name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ServerName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerInfo"/> class.
        /// </summary>
        /// <param name="serverName">Server name.</param>
        /// <param name="messageVersion">Server message schema version.</param>
        /// <param name="maxPingTime">Ping timeout.</param>
        /// <param name="id">Message ID.</param>
        public ServerInfo(string serverName, uint messageVersion, uint maxPingTime, uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
            ServerName = serverName;
            MessageVersion = messageVersion;
            MaxPingTime = maxPingTime;
        }
    }
}