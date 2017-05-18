using System;
using Buttplug.Logging;
using Buttplug.Messages;

namespace Buttplug.Core
{
    internal class ButtplugLog : IButtplugLog
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
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Trace, aMsg));
        }

        public void Debug(string aMsg)
        {
            _log.Debug(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Debug, aMsg));
        }
         
        public void Info(string aMsg)
        {
            _log.Info(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Info, aMsg));
        }

        public void Warn(string aMsg)
        {
            _log.Warn(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Warn, aMsg));
        }

        public void Error(string aMsg)
        {
            _log.Error(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Error, aMsg));
        }

        public void Fatal(string aMsg)
        {
            _log.Fatal(aMsg);
            LogMessageReceived?.Invoke(this, new ButtplugLogMessageEventArgs(ButtplugLogLevel.Fatal, aMsg));
        }

        public Error LogErrorMsg(uint aId, string msg)
        {
            Error(msg);
            return new Error(msg, aId);
        }

        public Error LogWarnMsg(uint aId, string msg)
        {
            Warn(msg);
            return new Error(msg, aId);
        }

        public Error LogInfoMsg(uint aId, string msg)
        {
            Info(msg);
            return new Error(msg, aId);
        }
    }
}
