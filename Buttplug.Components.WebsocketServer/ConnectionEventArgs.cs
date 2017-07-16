using JetBrains.Annotations;

namespace Buttplug.Components.WebsocketServer
{
    public class ConnectionEventArgs
    {
        [NotNull]
        public string ConnId;

        [NotNull]
        public string ClientName;

        public ConnectionEventArgs(string aConnId, string aClientName = "Unknown Client")
        {
            ConnId = aConnId;
            ClientName = aClientName;
        }
    }
}