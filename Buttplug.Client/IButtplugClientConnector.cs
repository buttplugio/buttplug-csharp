using System;
using System.Threading.Tasks;
using Buttplug.Core;

namespace Buttplug.Client
{
    public interface IButtplugClientConnector
    {
        Task Connect();
        Task Disconnect();
        Task<ButtplugMessage> Send(ButtplugMessage aMsg);
        bool Connected { get; }
        event EventHandler<MessageReceivedEventArgs> MessageReceived;
        event EventHandler Disconnected;
    }
}