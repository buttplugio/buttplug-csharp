namespace Buttplug.Core
{
    /// <summary>
    /// Constants for Buttplug
    /// </summary>
    public static class ButtplugConsts
    {
        /// <summary>
        /// The message ID for messages that originate from the server.
        /// </summary>
        public const uint SystemMsgId = 0;

        /// <summary>
        /// The default message ID for messages not originating from the server.
        /// In general, message IDs should incremented to prevent responses from clashing.
        /// The only time this is not an issue is if the server is being accessed directly as a library.
        /// </summary>
        public const uint DefaultMsgId = 1;
    }
}