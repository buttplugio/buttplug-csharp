using System;
using CommandLine;

namespace Buttplug.Server.CLI
{
    class Options
    {
        [Option("servername", Default = "Buttplug Server", HelpText = "Name of server to pass to connecting clients.")]
        public string ServerName { get; set; }

        [Option("serverversion", HelpText = "Print version and exit")]
        public bool Version { get; set; }

        [Option("generatecert", HelpText = "Generate certificate file at the path specified, then exit.")]
        public bool GenerateCertificate { get; set; }

        [Option("deviceconfig", HelpText = "Device Configuration file")]
        public string DeviceConfigFile { get; set; }

        [Option("userdeviceconfig", HelpText = "User Device Configuration file")]
        public string UserDeviceConfigFile { get; set; }

        [Option("websocketserver", HelpText = "Run websocket server")]
        public bool UseWebsocketServer { get; set; }

        [Option("websocketserverallinterfaces", Default = false, HelpText = "If passed, listen on all interfaces. Otherwise, only listen on 127.0.0.1.")]
        public bool WebsocketServerAllInterfaces { get; set; }

        [Option("insecureport", Default = 0, HelpText = "Insecure port for websocket servers.")]
        public int WebsocketServerInsecurePort { get; set; }

        [Option("secureport", Default = 0, HelpText = "Secure port for websocket servers. Requires certificate files also be passed.")]
        public int WebsocketServerSecurePort { get; set; }

        [Option("certfile", HelpText = "Certificate file (in PEM format) for secure Websocket Server")]
        public string CertFile { get; set; }

        [Option("privfile", HelpText = "Private Key file (in PEM format) for secure Websocket Server")]
        public string PrivFile { get; set; }

        [Option("ipcserver", HelpText = "Run ipc server")]
        public bool UseIpcServer { get; set; }

        [Option("ipcpipe", Default = "ButtplugPipe", HelpText = "Pipe name for IPC Server")]
        public string IpcPipe { get; set; }

        [Option("guipipe", Default = null, HelpText = "IPC Pipe name for GUI info")]
        public string GuiPipe { get; set; }

        [Option("pingtime", Default = 0, HelpText = "Ping timeout maximum for server (in milliseconds")]
        public int PingTime { get; set; }

        [Option("stayopen", HelpText = "If passed, server will stay running after client disconnection")]
        public bool StayOpen { get; set; }

        [Option("log", HelpText = "Print logs to console")]
        public string Log { get; set; }
    }
}
