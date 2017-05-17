using Buttplug.Messages;

namespace Buttplug.Core
{
    public class ButtplugUtils
    {
        public static Error LogErrorMsg(uint aId, ButtplugLog l, string msg)
        {
            l.Error(msg);
            return new Error(msg, aId);
        }

        public static Error LogWarnMsg(uint aId, ButtplugLog l, string msg)
        {
            l.Warn(msg);
            return new Error(msg, aId);
        }

        public static Error LogInfoMsg(uint aId, ButtplugLog l, string msg)
        {
            l.Info(msg);
            return new Error(msg, aId);
        }
    }
}