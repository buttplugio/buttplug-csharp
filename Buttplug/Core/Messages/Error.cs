using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Indicator that there has been an error in the system, either due to the last message/query
    /// sent, or due to an internal error.
    /// </summary>
    [ButtplugMessageMetadata("Error")]
    public class Error : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Types of errors described by the message.
        /// </summary>
        public enum ErrorClass
        {
            /// <summary>
            /// Error was caused by unknown factors.
            /// </summary>
            ERROR_UNKNOWN,

            /// <summary>
            /// Error was caused during connection handshake.
            /// </summary>
            ERROR_INIT,

            /// <summary>
            /// Max ping timeout has been exceeded.
            /// </summary>
            ERROR_PING,

            /// <summary>
            /// Error parsing messages.
            /// </summary>
            ERROR_MSG,

            /// <summary>
            /// Error at the device manager level (device doesn't exist/disconnected, etc...)
            /// </summary>
            ERROR_DEVICE,
        }

        /// <summary>
        /// Specific error type this message describes.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ErrorClass ErrorCode;

        /// <summary>
        /// Human-readable error description.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class. The message ID may be zero
        /// if raised outside of servicing a message from the client, otherwise the message ID must
        /// match the message being serviced.
        /// </summary>
        /// <param name="errorMessage">Human-readable error description.</param>
        /// <param name="errorCode">Class of error.</param>
        /// <param name="id">Message ID.</param>
        public Error(string errorMessage, ErrorClass errorCode, uint id)
            : base(id)
        {
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }
    }
}