using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Buttplug;

namespace ButtplugCLI
{
    public class ButtplugServer : WebSocketBehavior
    {
        private ButtplugService mButtplug;
        public ButtplugServer()
        {
            mButtplug = new ButtplugService();
            mButtplug.DeviceAdded += DeviceAddedHandler;
            mButtplug.MessageReceived += OnMessageReceived;
            mButtplug.StartScanning();
        }

        public void DeviceAddedHandler(object o, DeviceAddedEventArgs e)
        {
            Console.WriteLine("Found a device!");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("Got a message! " + e.Data);
            base.OnMessage(e);
            mButtplug.SendMessage(e.Data);
        }

        public void OnMessageReceived(object o, MessageReceivedEventArgs e)
        {
            Console.WriteLine(e.Message);
            ButtplugJSONMessageParser.Serialize(e.Message).IfSome(x => Console.WriteLine(x));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var p = new Program();
            var wssv = new WebSocketServer(6868);
            wssv.Log.Level = LogLevel.Trace;
            wssv.AddWebSocketService<ButtplugServer>("/Buttplug");

            wssv.Start();
            Console.ReadKey(true);
            wssv.Stop();
        }
    }
}
