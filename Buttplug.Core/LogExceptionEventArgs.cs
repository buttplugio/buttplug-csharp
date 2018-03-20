using System;

namespace Buttplug.Core
{
    /// <summary>
    /// Event wrapper for an exception log message
    /// </summary>
    public class LogExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the exception
        /// </summary>
        public Exception Ex { get; }

        /// <summary>
        /// Is the error local only, or can we send this to the client (assuming they want it)
        /// </summary>
        public bool LocalOnly { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="aEx"></param>
        /// <param name="aLocalOnly"></param>
        /// <param name="aErrorMessage"></param>
        public LogExceptionEventArgs(Exception aEx, bool aLocalOnly, string aErrorMessage)
        {
            ErrorMessage = aErrorMessage;
            Ex = aEx;
            LocalOnly = aLocalOnly;
        }
    }
}