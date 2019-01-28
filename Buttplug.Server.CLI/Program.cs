using CommandLine;

namespace Buttplug.Server.CLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var server = new ServerCLI();

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(server.RunServer);
        }
    }
}
