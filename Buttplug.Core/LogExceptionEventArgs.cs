using System;

namespace Buttplug.Core
{
    /// <summary>
    /// Event wrapper for an exception log message
    /// </summary>
    public class LogExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Error description.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Exception object related to error.
        /// </summary>
        public Exception Ex { get; }

        /// <summary>
        /// If true, error should not be forwarded to client.
        /// </summary>
        public bool LocalOnly { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="aEx">Exception to log</param>
        /// <param name="aLocalOnly">If true, do not send error message to client</param>
        /// <param name="aErrorMessage">Description of the error</param>
        public LogExceptionEventArgs(Exception aEx, bool aLocalOnly, string aErrorMessage)
        {
            ErrorMessage = aErrorMessage;
            Ex = aEx;
            LocalOnly = aLocalOnly;
        }
    }
}