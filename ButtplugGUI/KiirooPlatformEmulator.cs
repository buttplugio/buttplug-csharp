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

namespace ButtplugGUI
{
    public class KiirooToLaunchConverter
    {
        private Stopwatch _stopwatch;
        private ushort _previousSpeed;
        private ushort _previousPosition;
        private ushort _previousKiirooPosition;
        private ushort _limitedSpeed;

        public KiirooToLaunchConverter()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            _previousSpeed = 0;
            _previousPosition = 0;
            _limitedSpeed = 0;
        }

        public FleshlightLaunchRawCmd Convert(KiirooRawCmd aKiirooCmd)
        {
            var elapsed = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Stop();
            var kiirooPosition = aKiirooCmd.Position;
            if (kiirooPosition == _previousKiirooPosition)
            {
                return new FleshlightLaunchRawCmd(0, 0, _previousPosition);
            }
            _previousKiirooPosition = kiirooPosition;
            ushort speed = 0;

            // Speed Conversion
            if (elapsed > 2000)
            {
                speed = 50;
            }
            else if (elapsed > 1000)
            {
                speed = 20;
            }
            else
            {
                speed = (ushort)(100 - ((elapsed / 100) + ((elapsed / 100) * .1)));
                if (speed > _previousSpeed)
                {
                    speed = (ushort)(_previousSpeed + ((speed - _previousSpeed) / 6));
                }
                else if (speed <= _previousSpeed)
                {
                    speed = (ushort)(_previousSpeed - (speed / 2));
                }
            }
            if (speed < 20)
            {
                speed = 20;
            }
            _stopwatch.Start();
            // Position Conversion
            if (elapsed <= 150)
            {
                if (_limitedSpeed == 0)
                {
                    _limitedSpeed = speed;
                }
                ushort position = (ushort)(kiirooPosition > 2 ? 95 : 5);
                return new FleshlightLaunchRawCmd(0, _limitedSpeed, position);
            }
            else
            {
                _limitedSpeed = 0;
                ushort position = (ushort)(kiirooPosition > 2 ? 95 : 5);
                return new FleshlightLaunchRawCmd(0, speed, position);
            }
        }
    }

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
        private ButtplugService _service;
        private KiirooToLaunchConverter _converter;

        public SendDataHandler(ButtplugService service)
        {
            _service = service;
            _converter = new KiirooToLaunchConverter();
        }

        public async Task Handle(IHttpContext aContext, Func<Task> aNext)
        {
            var memoryStream = new MemoryStream();
            var w = new StreamWriter(memoryStream);
            w.Write("{}");
            w.Flush();
            var h = new Dictionary<string, string> { { "Access-Control-Allow-Origin", "*" } };
            aContext.Response = new HttpResponse(HttpResponseCode.Ok, "application/json; charset=utf-8", memoryStream, true, h);
            var position = ushort.Parse(aContext.Request.Post.Parsed.GetByName("data"));
            Debug.WriteLine(aContext.Request.Post.Parsed.GetByName("data"));
            await _service.SendMessage(_converter.Convert(new KiirooRawCmd(0, position)));
            await Task.Factory.GetCompleted();
        }
    }

    internal class KiirooPlatformEmulator
    {
        private HttpServer _httpServer;

        public void RunServer()
        {
            var httpServer = new HttpServer(new HttpRequestProvider());
            var server = new ButtplugService();
            server.SendMessage(new StartScanning());
            // Normal port 80 :
            _httpServer.Use(new TcpListenerAdapter(new TcpListener(IPAddress.Loopback, 6969)));

            // Handler classes :
            _httpServer.Use(new HttpRouter()
                    .With("senddata", new SendDataHandler(server))
                    .With("status", new SendStatusHandler()));

            _httpServer.Start();
        }

        public void StopServer()
        {
            _httpServer = null;
        }
    }
}