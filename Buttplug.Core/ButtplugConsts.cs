namespace Buttplug.Core
{
    /// <summary>
    /// Constants for Buttplug
    /// </summary>
    public static class ButtplugConsts
    {
        /// <summary>
        /// Default ID for server originated messages.
        /// </summary>
        public const uint SystemMsgId = 0;

        /// <summary>
        /// Default message ID for messages not originating from the server. In remote client/server
        /// environments, message IDs should be unique (usually monotonically increasing), to prevent
        /// responses from clashing.
        /// </summary>
        public const uint DefaultMsgId = 1;
    }
}