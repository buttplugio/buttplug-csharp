using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using Buttplug.Core;

namespace Buttplug.Server.Managers.ETSerialManager
{

    public class ET312HandshakeException : Exception
    {
        public ET312HandshakeException()
        {
        }

        public ET312HandshakeException(string message)
            : base(message)
        {
        }

        public ET312HandshakeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class ET312Device : ButtplugDevice
    {
        public ET312Device(SerialPort port, IButtplugLogManager aLogManager, string name, string id)
            : base(aLogManager, name, id)
        {


        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }
    }
}
