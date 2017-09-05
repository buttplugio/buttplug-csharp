using System;

namespace Buttplug.Core
{
    public class LogExceptionEventArgs : EventArgs
    {
        public LogExceptionEventArgs(Exception aEx, bool aLocalOnly, string aErrorMessage)
        {
            ErrorMessage = aErrorMessage;
            Ex = aEx;
            LocalOnly = aLocalOnly;
        }

        public string ErrorMessage { get; }

        public Exception Ex { get; }

        public bool LocalOnly { get; }
    }
}