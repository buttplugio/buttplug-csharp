namespace Buttplug.Core
{
    /// <summary>
    /// Logging levels used by the <see cref="ButtplugLog"/>
    /// </summary>
    public enum ButtplugLogLevel : byte
    {
        /// <summary>
        /// Logging disabled
        /// </summary>
        Off,

        /// <summary>
        /// Critical errors
        /// </summary>
        Fatal,

        /// <summary>
        /// Non-critical errors
        /// </summary>
        Error,

        /// <summary>
        /// Warnings
        /// </summary>
        Warn,

        /// <summary>
        /// Useful information
        /// </summary>
        Info,

        /// <summary>
        /// Finer grained debug logging
        /// </summary>
        Debug,

        /// <summary>
        /// Highly detailed and noisy logging for debugging
        /// </summary>
        Trace,
    }
}