using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;

namespace Buttplug.Client.Connectors.IPCConnector
{
    public class ButtplugClientIPCConnector : ButtplugRemoteJSONConnector, IButtplugClientConnector
    {
        public bool Connected => false;

        public event EventHandler Disconnected;

        /// <summary>
        /// Used for dispatching events to the owning application context.
        /// </summary>
        private readonly SynchronizationContext _owningDispatcher = SynchronizationContext.Current ?? new SynchronizationContext();

        private readonly string _ipcSocketName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aURL">
        /// The URL for the Buttplug WebSocket Server, in the form of wss://address:port (wss:// is
        /// to ws:// as https:// is to http://)
        /// </param>
        /// <param name="aIgnoreSSLErrors">
        /// When using SSL (wss://), prevents bad certificates from causing connection failures
        /// </param>
        public ButtplugClientIPCConnector(string aIPCSocketName)
        {
            _ipcSocketName = aIPCSocketName;
        }

        /// <summary>
        /// Creates the connection to the Buttplug Server and performs the protocol handshake.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public async Task Connect()
        {
        }

        /// <summary>
        /// Closes the WebSocket Connection.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public async Task Disconnect()
        {
            _owningDispatcher.Send(_ => Disconnected?.Invoke(this, new EventArgs()), null);
        }

        public async Task<ButtplugMessage> Send(ButtplugMessage aMsg)
        {
            return null;
        }
    }
}
