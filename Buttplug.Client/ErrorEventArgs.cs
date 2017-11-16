using System;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    public class ErrorEventArgs
    {
        [NotNull]
        public readonly Error Message;

        [NotNull]
        public readonly Exception Exception;

        public ErrorEventArgs(Error aMsg)
        {
            Message = aMsg;
            Exception = null;
        }

        public ErrorEventArgs(Exception aException)
        {
            Exception = aException;
            Message = new Error(Exception.Message, Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId);
        }
    }
}