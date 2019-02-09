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
            });
            parser.ParseArguments<Options>(args).WithParsed(server.RunServer);
        }
    }
}
