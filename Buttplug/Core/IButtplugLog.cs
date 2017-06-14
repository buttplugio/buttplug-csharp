using System;
using Buttplug.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public interface IButtplugLog
    {
        [CanBeNull]
        event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;
        void Trace(string aMsg, bool localOnly = false);
        void Debug(string aMsg, bool localOnly = false);
        void Info(string aMsg, bool localOnly = false);
        void Warn(string aMsg, bool localOnly = false);
        void Error(string aMsg, bool localOnly = false);
        void Fatal(string aMsg, bool localOnly = false);
        [NotNull]
        Error LogErrorMsg(uint aId, string aMsg);
        [NotNull]
        Error LogWarnMsg(uint aId, string aMsg);
    }
}
