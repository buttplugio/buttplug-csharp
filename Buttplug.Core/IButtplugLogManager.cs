using System;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    /// <summary>
    /// The Buttplug logging manager.
    /// This handles recieving log messages and reporting any that match the
    /// granularity level to any listeners
    /// </summary>
    public interface IButtplugLogManager
    {
        /// <summary>
        /// This handler is called when a log message has been recieved
        /// </summary>
        [CanBeNull]
        event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;

        /// <summary>
        /// The level of log to report
        /// </summary>
        ButtplugLogLevel Level { set; }

        /// <summary>
        /// Gets a Buttplug logger for the specified type
        /// </summary>
        /// <param name="aType">The type this logger is for</param>
        /// <returns>A Buttplug logger</returns>
        [NotNull]
        IButtplugLog GetLogger(Type aType);
    }
}
