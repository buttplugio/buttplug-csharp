using CommandLine;

namespace Buttplug.Server.CLI
{
    class Options
    {
        [Option("name", Default = "Buttplug Server", HelpText = "Name of server to pass to connecting clients.")]
        public string ServerName { get; set; }

        [Option("version", HelpText = "Print version and exit")]
        public bool Version { get; set; }

        [Option("deviceconfig", HelpText = "Device Configuration file")]
        public string DeviceConfigFile { get; set; }

        [Option("userdeviceconfig", HelpText = "User Device Configuration file")]
        public string UserDeviceConfigFile { get; set; }

        [Option("websocketserver", HelpText = "Run websocket server")]
        public bool WebsocketServer { get; set; }

        [Option("host", Default = "localhost", HelpText = "Host for websocket servers")]
        public string Host { get; set; }

        [Option("port", Default = 12345, HelpText = "Port for websocket servers")]
        public int Port { get; set; }

        [Option("websocketsecure", Default = false, HelpText = "Generate and use certificate for Websocket Server")]
        public bool WebsocketSecure { get; set; }

        [Option("certfile", HelpText = "Certificate for secure Websocket Server")]
        public string CertFile { get; set; }

        [Option("ipcserver", HelpText = "Run ipc server")]
        public bool IpcServer { get; set; }

        [Option("guipipe", Default = null, HelpText = "IPC Pipe name for GUI info")]
        public string GuiPipe { get; set; }

        [Option("ipcpipe", Default = "ButtplugPipe", HelpText = "Pipe name for IPC Server")]
        public string IpcPipe { get; set; }

        [Option("pingtime", Default = 0, HelpText = "Ping timeout maximum for server (in milliseconds")]
        public int PingTime { get; set; }

        [Option("stayopen", HelpText = "If passed, server will stay running after client disconnection")]
        public bool StayOpen { get; set; }

        [Option("log", HelpText = "Print logs to console")]
        public string Log { get; set; }
    }
}
