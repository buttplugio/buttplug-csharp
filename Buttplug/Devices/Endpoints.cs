namespace Buttplug.Devices
{
    public static class Endpoints
    {
        /// <summary>
        /// Default outgoing endpoint name.
        /// </summary>
        public static string Tx = "tx";

        /// <summary>
        /// Default incoming endpoint name.
        /// </summary>
        public static string Rx = "rx";

        /// <summary>
        /// Command endpoint name. Used on some Kiiroo devices.
        /// </summary>
        public static string Command = "command";

        /// <summary>
        /// Generic name for devices that have a firmware loading endpoint.
        /// </summary>
        public static string Firmware = "firmware";
    }
}