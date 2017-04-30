using System;
using Buttplug;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Threading.Tasks;
using Buttplug.Messages;

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
            mButtplug.DeviceAdded += DeviceAddedHandler;
            mButtplug.StartScanning();
        }

        public void DeviceAddedHandler(object o, DeviceAddedEventArgs e)
        {
            Console.WriteLine("Found a device!");
            Task.Delay(1000).ContinueWith(t => this.SendGamepadOn());
        }

        public void SendGamepadOn()
        {
            SingleMotorVibrateMessage m = new SingleMotorVibrateMessage(0, 0.8);
            if(!mButtplug.SendMessage(m))
            {
                Console.WriteLine("Can't send on message!");
                return;
            }
            Task.Delay(1000).ContinueWith(t => this.SendGamepadOff());
        }

        public void SendGamepadOff()
        {
            SingleMotorVibrateMessage m = new SingleMotorVibrateMessage(0, 0);
            if (!mButtplug.SendMessage(m))
            {
                Console.WriteLine("Can't send off message!");
                return;
            }
            Task.Delay(1000).ContinueWith(t => this.SendGamepadOn());
        }
    }
}
