using System;
using JetBrains.Annotations;

namespace Buttplug.Core.Logging
{
    /// <summary>
    /// Handles receiving log messages, and reports any that match requested granularity levels
    /// to be sent to clients.
    /// </summary>
    public interface IButtplugLogManager
    {
        /// <summary>
        /// Called when a log message has been received.
        /// </summary>
        [CanBeNull]
        event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;

        /// <summary>
        /// Sets the log level to report.
        /// </summary>
        ButtplugLogLevel Level { get; set; }

        /// <summary>
        /// Gets a <see cref="ButtplugLog"/> for the specified type. Used for creating loggers specific to
        /// class types, so the types can be prepended to the log message for tracing.
        /// </summary>
        /// <param name="aType">Type that this logger will be for</param>
        /// <returns>Logger object</returns>
        [NotNull]
        IButtplugLog GetLogger(Type aType);
    }
}
