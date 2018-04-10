using JetBrains.Annotations;

namespace Buttplug.Components.IPCServer
{
    public class IPCConnectionEventArgs
    {
        [NotNull]
        public string ClientName;

        public IPCConnectionEventArgs(string aClientName = "Unknown Client")
        {
            ClientName = aClientName;
        }
    }
}