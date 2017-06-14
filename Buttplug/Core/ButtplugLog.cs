using System;
using Buttplug.Logging;
using Buttplug.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    internal class ButtplugLog : IButtplugLog
    {
        [NotNull]
        private readonly ILog _log;
        public event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;

        public ButtplugLog([NotNull] ILog aLogger)
        {
            _log = aLogger;
        }

        public void Trace(string aMsg, bool localOnly)
        {
            _log.Trace(aMsg);
            if (!localOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Trace, aMsg));
            }
        }

        public void Debug(string aMsg, bool localOnly)
        {
            _log.Debug(aMsg);
            if (!localOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Debug, aMsg));
            }
        }

        public void Info(string aMsg, bool localOnly)
        {
            _log.Info(aMsg);
            if (!localOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Info, aMsg));
            }
        }

        public void Warn(string aMsg, bool localOnly)
        {
            _log.Warn(aMsg);
            if (!localOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Warn, aMsg));
            }
        }

        public void Error(string aMsg, bool localOnly)
        {
            _log.Error(aMsg);
            if (!localOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Error, aMsg));
            }
        }

        public void Fatal(string aMsg, bool localOnly)
        {
            _log.Fatal(aMsg);
            if (!localOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Fatal, aMsg));
            }
        }

        public Error LogErrorMsg(uint aId, string msg)
        {
            Error(msg, false);
            return new Error(msg, aId);
        }

        public Error LogWarnMsg(uint aId, string msg)
        {
            Warn(msg, false);
            return new Error(msg, aId);
        }
    }
}
