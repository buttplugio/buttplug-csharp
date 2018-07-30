using System;
using System.Collections.Generic;
using System.Threading;
using Buttplug.Components.WebsocketServer;
using CommandLine;

namespace Buttplug.Apps.ServerCLI
{
    class Program
    {
        private static void Run(Options aOpts)
        {
            var ws = new ButtplugWebsocketServer();
            Console.CancelKeyPress += delegate
            {
                ws.StopServer();
                Console.WriteLine("Exiting server");
            };
            ws.StartServer(new BPServerFactory(), (int)aOpts.port, aOpts.host == "localhost", aOpts.ssl, aOpts.host);
            while (ws.IsConnected)
            {
                Thread.Sleep(500);
            }
        }

        private static void Error(IEnumerable<Error> aErrors)
        {
            Console.WriteLine("Error starting BP CLI Server");
            foreach (var error in aErrors)
            {
                Console.WriteLine(error);
            }
        }

        static void Main(string[] args)
        {
            CommandLine.Parser.Default
                .ParseArguments<Options>(args)
                .WithParsed(Run)
                .WithNotParsed(Error);
        }
    }
}
