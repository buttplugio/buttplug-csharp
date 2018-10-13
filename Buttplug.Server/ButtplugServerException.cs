using System;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Server
{
    public class ButtplugServerException : ButtplugException
    {
        /// <inheritdoc />
        public ButtplugServerException(string aMessage, uint aId, Error.ErrorClass aClass, Exception aInner = null)
            : base(aMessage, aClass, aId, aInner)
        {
        }

        /// <inheritdoc />
        public ButtplugServerException([NotNull] IButtplugLog aLogger, string aMessage, uint aId, Error.ErrorClass aClass, Exception aInner = null)
            : base(aLogger, aMessage, aClass, aId, aInner)
        {
        }
    }
}