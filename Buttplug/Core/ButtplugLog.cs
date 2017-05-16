using System;
using Buttplug.Logging;

namespace Buttplug.Core
{
    internal class ButtplugLog
    {
        private readonly ILog _log;
        public event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;

        public ButtplugLog(ILog aLogger)
        {
            _log = aLogger;
        }

        public void Trace(string aMsg)
        {
            _log.Trace(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs("Trace", aMsg));
        }

        public void Debug(string aMsg)
        {
            _log.Debug(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs("Debug", aMsg));
        }
         
        public void Info(string aMsg)
        {
            _log.Info(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs("Info", aMsg));
        }

        public void Warn(string aMsg)
        {
            _log.Warn(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs("Warn", aMsg));
        }

        public void Error(string aMsg)
        {
            _log.Error(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs("Error", aMsg));
        }

        public void Fatal(string aMsg)
        {
            _log.Fatal(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs("Fatal", aMsg));
        }

    }
}
