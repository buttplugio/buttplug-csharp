using System;
using CommandLine;

namespace Buttplug.Apps.ServerCLI
{
    class Options
    {
        [Option("ssl", Default = false, HelpText = "Use SSL")]
        public bool ssl { get; set; }

        [Option("host", Default = "localhost", HelpText = "Host address to use")]
        public string host { get; set; }

        [Option("port", Default = 12345, HelpText = "Port to listen on")]
        public int port { get; set; }
    }
}
