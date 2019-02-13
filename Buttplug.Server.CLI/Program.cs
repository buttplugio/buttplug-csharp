using System;
using CommandLine;

namespace Buttplug.Server.CLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var server = new ServerCLI();
            var parser = new Parser(with =>
            {
                with.EnableDashDash = true;
                with.AutoVersion = false;
                with.AutoHelp = true;
                with.HelpWriter = Console.Error;
            });
            parser.ParseArguments<Options>(args).WithParsed(server.RunServer);
        }
    }
}
