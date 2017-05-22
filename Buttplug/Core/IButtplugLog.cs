using System;
using Buttplug.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public interface IButtplugLog
    {
        [CanBeNull]
        event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;
        void Trace(string aMsg);
        void Debug(string aMsg);
        void Info(string aMsg);
        void Warn(string aMsg);
        void Error(string aMsg);
        void Fatal(string aMsg);
        [NotNull]
        Error LogErrorMsg(uint aId, string aMsg);
        [NotNull]
        Error LogWarnMsg(uint aId, string aMsg);
    }
}
