using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Buttplug;
using CommandLine;
using CommandLine.Text;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace ButtplugCLI
{
    public class Options
    {
        private static readonly HeadingInfo HeadingInfo = new HeadingInfo("ButtplugCLI", "0.0.1");
        [Option('p', "port", DefaultValue = 6868,
                HelpText = "Port for Websocket Server")]
        public int WebsocketPort { get; set; }

        [Option('l', "log", DefaultValue = "Warn",
                HelpText = "Log output level (Debug, Info, Warn, Error, Fatal)")]
        public string LogLevel { get; set; }

        [Option('q', "quiet", DefaultValue = false,
                HelpText = "If passed, no output will occur to stdout/stderr")]
        public bool Quiet { get; set; }

        [Option("exitimmediately", DefaultValue = false,
                HelpText = "Startup and exit. Only used to installer testing.")]
        public bool ExitImmediately { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            HelpText t = HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            t.Heading = HeadingInfo;
            t.Copyright = "Command Line Server for Haptics Routing and Translation";
            return t;
        }
    }

    public class ButtplugServer : WebSocketBehavior
    {

        private ButtplugService mButtplug;
        public ButtplugServer()
        {
            mButtplug = new ButtplugService();
            mButtplug.DeviceAdded += DeviceAddedHandler;
            mButtplug.MessageReceived += OnMessageReceived;
            mButtplug.StartScanning();
        }

        public void DeviceAddedHandler(object o, DeviceAddedEventArgs e)
        {
            Console.WriteLine("Found a device!");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("Got a message! " + e.Data);
            base.OnMessage(e);
            mButtplug.SendMessage(e.Data);
        }

        public void OnMessageReceived(object o, MessageReceivedEventArgs e)
        {
            Console.WriteLine(e.Message);
            ButtplugJSONMessageParser.Serialize(e.Message).IfSome(x => Console.WriteLine(x));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            var rule1 = new LoggingRule("*", NLog.LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(rule1);
            LogManager.Configuration = config;
            NLog.Logger BPLogger = LogManager.GetLogger("Buttplug");
            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                return;
            }
            if (options.ExitImmediately)
            {
                return;
            }
            var p = new Program();
            
            BPLogger.Info("Buttplug v0.0.1 - Booting up...");
            BPLogger.Info("Starting Websocket server on port " + options.WebsocketPort);
            var wssv = new WebSocketServer(options.WebsocketPort);
            if (options.Quiet)
            {
                wssv.Log.Level = WebSocketSharp.LogLevel.Fatal;
            }
            else
            {
                wssv.Log.Level = WebSocketSharp.LogLevel.Warn;
            }
            
            wssv.AddWebSocketService<ButtplugServer>("/Buttplug");

            wssv.Start();
            Console.ReadKey(true);
            wssv.Stop();
        }
    }
}
