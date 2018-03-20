using System;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    /// <summary>
    /// Event wrapper for a log message
    /// </summary>
    public class ButtplugLogMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the log message
        /// </summary>
        [NotNull]
        public Log LogMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugLogMessageEventArgs"/> class.
        /// </summary>
        /// <param name="aLogLevel">The log level</param>
        /// <param name="aMessage">The log message</param>
        public ButtplugLogMessageEventArgs(ButtplugLogLevel aLogLevel, string aMessage)
        {
            LogMessage = new Log(aLogLevel, aMessage);
        }
    }
}
