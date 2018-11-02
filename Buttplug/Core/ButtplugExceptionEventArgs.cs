using System;

namespace Buttplug.Core
{
    public class ButtplugExceptionEventArgs : EventArgs
    {
        public ButtplugException Exception { get; }

        public ButtplugExceptionEventArgs(ButtplugException ex)
        {
            Exception = ex;
        }
    }
}
