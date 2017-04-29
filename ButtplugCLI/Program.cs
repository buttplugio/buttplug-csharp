using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug;

namespace ButtplugCLI
{
    class Program
    {
        private ButtplugService mButtplug;
        static void Main(string[] args)
        {
            var p = new Program();
            Console.ReadLine();
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
