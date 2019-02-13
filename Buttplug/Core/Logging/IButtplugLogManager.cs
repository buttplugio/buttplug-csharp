using System;
using Buttplug.Core.Messages;
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
        /// Gets a <see cref="ButtplugLog"/> for the specified type. Used for creating loggers specific to
        /// class types, so the types can be prepended to the log message for tracing.
        /// </summary>
        /// <param name="aType">Type that this logger will be for.</param>
        /// <returns>Logger object.</returns>
        [NotNull]
        IButtplugLog GetLogger(Type aType);

        void AddLogListener(ButtplugLogLevel aLevel, Action<Log> aListener);
        void RemoveLogListener(Action<Log> aListener);

        ButtplugLogLevel MaxLevel { get; }
    }
}
