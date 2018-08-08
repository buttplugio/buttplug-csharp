using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;

namespace Buttplug.Client
{
    public interface IButtplugClientConnector
    {
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        event EventHandler Disconnected;

        Task ConnectAsync(CancellationToken aToken = default(CancellationToken));

        Task DisconnectAsync(CancellationToken aToken = default(CancellationToken));

        Task<ButtplugMessage> SendAsync(ButtplugMessage aMsg, CancellationToken aToken = default(CancellationToken));

        bool Connected { get; }
    }
}