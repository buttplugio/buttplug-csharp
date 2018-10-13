using System;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    /// <summary>
    /// Exceptions related to message serialization and deserialization. All Parser exceptions will
    /// be of <see cref="Error"/> class ERROR_MSG.
    /// </summary>
    public class ButtplugParserException : ButtplugException
    {
        /// <inheritdoc />
        public ButtplugParserException(string aMessage, uint aId, Exception aInner = null) :
            base(aMessage, Error.ErrorClass.ERROR_MSG, aId, aInner)
        {
        }

        /// <inheritdoc />
        public ButtplugParserException([NotNull] IButtplugLog aLogger, string aMessage, uint aId, Exception aInner = null) :
            base(aLogger, aMessage, Error.ErrorClass.ERROR_MSG, aId, aInner)
        {
        }
    }
}
