using System;
using Buttplug.Logging;
using JetBrains.Annotations;

namespace Buttplug.Core.Logging
{
    internal class ButtplugLog : IButtplugLog
    {
        [NotNull]
        private readonly ILog _log;

        public event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;

        public ButtplugLog([NotNull] ILog aLogger)
        {
            _log = aLogger ?? throw new ArgumentNullException(nameof(aLogger));
        }

        public void Trace(string aMsg, bool aLocalOnly)
        {
            _log.Trace(aMsg);
            if (!aLocalOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Trace, aMsg));
            }
        }

        public void Debug(string aMsg, bool aLocalOnly)
        {
            _log.Debug(aMsg);
            if (!aLocalOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Debug, aMsg));
            }
        }

        public void Info(string aMsg, bool aLocalOnly)
        {
            _log.Info(aMsg);
            if (!aLocalOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Info, aMsg));
            }
        }

        public void Warn(string aMsg, bool aLocalOnly)
        {
            _log.Warn(aMsg);
            if (!aLocalOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Warn, aMsg));
            }
        }

        public void Error(string aMsg, bool aLocalOnly)
        {
            _log.Error(aMsg);
            if (!aLocalOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Error, aMsg));
            }
        }

        public void Fatal(string aMsg, bool aLocalOnly)
        {
            _log.Fatal(aMsg);
            if (!aLocalOnly)
            {
                LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Fatal, aMsg));
            }
        }
    }
}
