using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Buttplug.Core
{
    internal class ButtplugUtils
    {
        public static IBuffer WriteString(string s)
        {
            var w = new DataWriter();
            w.WriteString(s);
            return w.DetachBuffer();
        }

        public static IBuffer WriteByteArray(byte[] b)
        {
            var w = new DataWriter();
            w.WriteBytes(b);
            return w.DetachBuffer();
        }
    }
}
