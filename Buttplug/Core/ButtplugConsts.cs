namespace Buttplug.Core
{
    /// <summary>
    /// Buttplug library constants
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
        /// responses from colliding.
        /// </summary>
        public const uint DefaultMsgId = 1;

        /// <summary>
        /// Spec version this Buttplug library is based on.
        /// </summary>
        public const uint CurrentSpecVersion = 1;
    }
}