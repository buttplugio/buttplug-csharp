using Buttplug.Messages;
using Windows.Storage.Streams;
using NLog;

namespace Buttplug.Core
{
    public class ButtplugUtils
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

        public static Error LogAndError(Logger l, LogLevel level, string msg)
        {
            l.Log(level, msg);
            return new Error(msg);
        }
    }
}
