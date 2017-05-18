using System;
using Buttplug.Messages;

namespace Buttplug.Core
{
    public interface IButtplugLog
    {
        event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;
        void Trace(string aMsg);
        void Debug(string aMsg);
        void Info(string aMsg);
        void Warn(string aMsg);
        void Error(string aMsg);
        void Fatal(string aMsg);
        Error LogErrorMsg(uint aId, string aMsg);
        Error LogWarnMsg(uint aId, string aMsg);
    }
}
