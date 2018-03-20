using System;
using Buttplug.Core.Messages;
using JetBrains.Annotations;
using static Buttplug.Core.Messages.Error;

namespace Buttplug.Core
{
    /// <summary>
    /// The Buttplug logger
    /// Tied to a specific type, this class emits log events
    /// </summary>
    public interface IButtplugLog
    {
        /// <summary>
        /// Emits a Trace log event
        /// </summary>
        /// <param name="aMsg">The log message</param>
        /// <param name="aLocalOnly">Do not send the log message to the client</param>
        void Trace(string aMsg, bool aLocalOnly = false);

        /// <summary>
        /// Emits a Debug log event
        /// </summary>
        /// <param name="aMsg">The log message</param>
        /// <param name="aLocalOnly">Do not send the log message to the client</param>
        void Debug(string aMsg, bool aLocalOnly = false);

        /// <summary>
        /// Emits a Info log event
        /// </summary>
        /// <param name="aMsg">The log message</param>
        /// <param name="aLocalOnly">Do not send the log message to the client</param>
        void Info(string aMsg, bool aLocalOnly = false);

        /// <summary>
        /// Emits a Warn log event
        /// </summary>
        /// <param name="aMsg">The log message</param>
        /// <param name="aLocalOnly">Do not send the log message to the client</param>
        void Warn(string aMsg, bool aLocalOnly = false);

        /// <summary>
        /// Emits an Error log event
        /// </summary>
        /// <param name="aMsg">The log message</param>
        /// <param name="aLocalOnly">Do not send the log message to the client</param>
        void Error(string aMsg, bool aLocalOnly = false);

        /// <summary>
        /// Emits a Fatal log event
        /// </summary>
        /// <param name="aMsg">The log message</param>
        /// <param name="aLocalOnly">Do not send the log message to the client</param>
        void Fatal(string aMsg, bool aLocalOnly = false);

        /// <summary>
        /// Emits a Error log event from an exception
        /// </summary>
        /// <param name="aEx">The exception</param>
        /// <param name="aLocalOnly">Do not send the log message to the client</param>
        /// <param name="aMsg">An optional log message</param>
        void LogException(Exception aEx, bool aLocalOnly = true, string aMsg = null);

        /// <summary>
        /// This handler is called when an exception log message has been recieved
        /// </summary>
        [CanBeNull]
        event EventHandler<LogExceptionEventArgs> OnLogException;
    }
}
