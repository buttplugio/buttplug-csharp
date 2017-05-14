using Buttplug.Core;
using Buttplug.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using uhttpsharp;
using uhttpsharp.Handlers;
using uhttpsharp.Listeners;
using uhttpsharp.RequestProviders;

namespace ButtplugKiirooPlatformEmulator
{

    public class SendStatusHandler : IHttpRequestHandler
    {
        public Task Handle(IHttpContext aContext, Func<Task> aNext)
        {
            var memoryStream = new MemoryStream();
            var w = new StreamWriter(memoryStream);
            w.Write("{\"connectedDeviceName\":\"ONYX\",\"bluetoothAddress\":\"8CDE52B866B5\",\"firmwareUpdateProgres\":0,\"remoteDevice\":\"not connected\",\"devicestatus\":\"NORMAL\",\"localDevice\":\"connected\",\"previousdevice_connectionurl\":\"btspp://8CDE52B866B5:1;authenticate=false;encrypt=false;master=false\",\"readOnlyMode\":false,\"streamToDeviceEnabled\":true,\"delay\":0,\"writeOnlyMode\":false,\"currentFW\":\"91\",\"waitingforusbcable\":true,\"bluetoothOn\":true,\"previousdevice_name\":\"ONYX\",\"uienabled\":true,\"newFWVersionAvailable\":false,\"previousdevice_bluetoothaddress\":\"8CDE52B866B5\",\"statusCode\":1}");
            w.Flush();
            var h = new Dictionary<string, string> { { "Access-Control-Allow-Origin", "*" } };
            aContext.Response = new HttpResponse(HttpResponseCode.Ok, "application/json; charset=utf-8", memoryStream, true, h);
            return Task.Factory.GetCompleted();
        }
    }

    public class SendDataHandler : IHttpRequestHandler
    {
        private EventHandler<KiirooPlatformEventArgs> _handler;

        public SendDataHandler(EventHandler<KiirooPlatformEventArgs> aHandler)
        {
            _handler = aHandler;
        }

        public async Task Handle(IHttpContext aContext, Func<Task> aNext)
        {
            var memoryStream = new MemoryStream();
            var w = new StreamWriter(memoryStream);
            w.Write("{}");
            w.Flush();
            var h = new Dictionary<string, string> { { "Access-Control-Allow-Origin", "*" } };
            aContext.Response = new HttpResponse(HttpResponseCode.Ok, "application/json; charset=utf-8", memoryStream, true, h);
            try
            {
                var position = ushort.Parse(aContext.Request.Post.Parsed.GetByName("data"));
                _handler?.Invoke(this, new KiirooPlatformEventArgs(position));
            }
            catch (FormatException)
            {
                // Swallow format exceptions, as sometimes scripts can send "undefined". 
            }
            await Task.Factory.GetCompleted();
        }
    }

    public class KiirooPlatformEventArgs : EventArgs
    {
        public ushort Position { get; private set; }

        public KiirooPlatformEventArgs(ushort p)
        {
            Position = p;
        }
    }


    public class KiirooPlatformEmulator
    {
        private HttpServer _httpServer;
        private TcpListener _tcpAdapter;
        public event EventHandler<KiirooPlatformEventArgs> OnKiirooPlatformEvent;

        public KiirooPlatformEmulator()
        {

        }

        public void StartServer()
        {
            _tcpAdapter = new TcpListener(IPAddress.Loopback, 6969);
            _httpServer = new HttpServer(new HttpRequestProvider());
            // Normal port 80 :
            _httpServer.Use(new TcpListenerAdapter(_tcpAdapter));

            // Handler classes :
            _httpServer.Use(new HttpRouter()
                .With("senddata", new SendDataHandler(OnKiirooPlatformEvent))
                .With("status", new SendStatusHandler()));
            _httpServer.Start();
        }

        public void StopServer()
        {
        }
    }
}