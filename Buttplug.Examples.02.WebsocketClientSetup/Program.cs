using System;
using System.Threading.Tasks;
using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;

// Tutorial file, disable ConfigureAwait checking since it's an actual program.
// ReSharper disable ConsiderUsingConfigureAwait

namespace Buttplug.Examples._02.WebsocketClientSetup
{
    internal class Program
    {
        private static async Task WaitForKey()
        {
            Console.WriteLine("Press any key to continue.");
            while (!Console.KeyAvailable)
            {
                await Task.Delay(1);
            }
            Console.ReadKey(true);
        }

        private static async Task RunExample()
        {
            // Welcome to the second example. Now, instead of embedding the server in the client,
            // we'll connect to an outside Buttplug Websocket Server.
            //
            // For this, you'll need to download and install the Buttplug C# Windows Suite, which you
            // can get at https://github.com/buttplugio/buttplug-csharp/releases.
            //
            // We don't have a .Net Standard version of this working quite yet, so if you're trying
            // this on Mac/Linux/etc, you'll either need a Windows machine somewhere on your network,
            // or else you can just follow along.
            //
            // Once you've got the Windows Suite installed, start the "Buttplug Server" application.
            // Hit "stop" on the server, and turn SSL/TLS off. Then hit start on the server again.
            // We'll be testing this without SSL for the moment. If you would like to use a web
            // application with the server, remember to turn SSL/TLS back on after this, as most
            // Buttplug web applications require HTTPS.

            // As with the last example, we'll need a connector first. This time, instead of holding
            // a server ourselves in the connector, the server will be located elsewhere. In this
            // case, it'll most likely be another process on the same computer, though remote
            // connections over networks are certainly possible.
            //
            // This time, instead of specifying a Server Name, we now specify a server network
            // address. The default server address is "ws://localhost:12345/buttplug", so we'll use
            // that. If you are trying to connect to another machine, you'll need to change this
            // address to point to that machine.
            var connector = new ButtplugWebsocketConnector(new Uri("ws://localhost:12345/buttplug"));

            // ButtplugClient creation is the same as the last example. From here on out, things look
            // basically the same.
            var client = new ButtplugClient("Example Client", connector);

            // And we connect the same as last time...
            try
            {
                await client.ConnectAsync();
            }
            catch (ButtplugClientConnectorException ex)
            {
                // If our connection failed, because the server wasn't turned on, SSL/TLS wasn't
                // turned off, etc, we'll just print and exit here. This will most likely be a
                // wrapped exception.
                Console.WriteLine($"Can't connect to Buttplug Server, exiting! Message: {ex.InnerException.Message}");
                await WaitForKey();
                return;
            }

            // We're connected, yay!
            Console.WriteLine("Connected! Check Server GUI for Client Name.");

            // We'll put in a wait for the user to hit Ctrl-C. That way, once you connect here,
            // you can look at the Server GUI and see that is says that "Example Client" is connected.
            await WaitForKey();

            // And now we disconnect as usual
            await client.DisconnectAsync();

            // That's it for remote settings. Congrats, you've vaguely sorta teledildonicsed! At
            // least with two processes on the same machine, but hey, that's remote, right?
        }

        // Since not everyone is probably going to want to run under C# 7.1+, we'll use a non-async
        // Main and call to a Wait()'d task. C# 8 can't come soon enough.
        private static void Main()
        {
            // Setup a client, and wait until everything is done before exiting.
            RunExample().Wait();
        }
    }
}