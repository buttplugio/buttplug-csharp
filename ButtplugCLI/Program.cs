using System;
using CommandLine;
using CommandLine.Text;
using NLog;
using NLog.Config;
using NLog.Targets;
using WebSocketSharp.Server;

namespace ButtplugCLI
{
    public class Options
    {
        private static readonly HeadingInfo HeadingInfo = new HeadingInfo("ButtplugCLI", "0.0.1");

        [Option('p', "port", DefaultValue = 6868,
                HelpText = "Port for Websocket Server")]

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int WebsocketPort { get; set; }

        [Option('l', "log", DefaultValue = "Warn",
                HelpText = "Log output level (Debug, Info, Warn, Error, Fatal)")]

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string LogLevel { get; set; }

        [Option('q', "quiet", DefaultValue = false,
                HelpText = "If passed, no output will occur to stdout/stderr")]

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool Quiet { get; set; }

        [Option("exitimmediately", DefaultValue = false,
                HelpText = "Startup and exit. Only used to installer testing.")]

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool ExitImmediately { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var t = HelpText.AutoBuild(this, aCur => HelpText.DefaultParsingErrorsHandler(this, aCur));
            t.Heading = HeadingInfo;
            t.Copyright = "Command Line Server for Haptics Routing and Translation";
            return t;
        }
    }

    internal static class Program
    {
        private static void Main(string[] aArgs)
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            var rule1 = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(rule1);
            LogManager.Configuration = config;
            var bpLogger = LogManager.GetLogger("Buttplug");
            var options = new Options();
            if (!Parser.Default.ParseArguments(aArgs, options))
            {
                return;
            }

            if (options.ExitImmediately)
            {
                return;
            }

            bpLogger.Info("Buttplug v0.0.1 - Booting up...");
            bpLogger.Info("Starting Websocket server on port " + options.WebsocketPort);
            var wssv = new WebSocketServer(options.WebsocketPort);
            wssv.Log.Level = options.Quiet ? WebSocketSharp.LogLevel.Fatal : WebSocketSharp.LogLevel.Warn;

            // wssv.AddWebSocketService<ButtplugWebsocketServer.ButtplugWebsocketServer>("/Buttplug");
            wssv.Start();
            Console.ReadKey(true);
            wssv.Stop();
        }
    }
}