using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ButtplugCLI
{
    public class ButtplugServer : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("Got a message!");
            base.OnMessage(e);
        }
    }

    class Program
    {
        private ButtplugService mButtplug;
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

        public Program()
        {
            mButtplug = new ButtplugService();
            mButtplug.DeviceFound += DeviceFoundHandler;
        }

        public void DeviceFoundHandler(object o, DeviceFoundEventArgs e)
        {
            Console.WriteLine("Found a device!");
        }

    }
}
