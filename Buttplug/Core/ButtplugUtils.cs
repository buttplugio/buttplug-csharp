using Buttplug.Messages;
using Buttplug.Logging;
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

        public static Error LogErrorMsg(uint aId, ILog l, string msg)
        {
            l.Error(msg);
            return new Error(msg, aId);
        }

        public static Error LogWarnMsg(uint aId, ILog l, string msg)
        {
            l.Warn(msg);
            return new Error(msg, aId);
        }

        public static Error LogInfoMsg(uint aId, ILog l, string msg)
        {
            l.Info(msg);
            return new Error(msg, aId);
        }
    }
}