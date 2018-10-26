using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Client;

// Tutorial file, disable ConfigureAwait checking since it's an actual program.
// ReSharper disable ConsiderUsingConfigureAwait

namespace Buttplug.Examples._03.ConnectionLifetimesAndPingTimers
{
    class Program
    {
        // Dummy client class that ignores ping timing, just used here for show.
        private class ButtplugNoPingTestClient : ButtplugClient
        {
            public ButtplugNoPingTestClient(IButtplugClientConnector aConnector)
                : base("TestClient", aConnector)
            {
            }

            public void StopPingTimer()
            {
                // Run connection, then just get rid of the ping timer.
                _pingTimer.Change(Timeout.Infinite, 100);
            }
        }

        private static async Task WaitForKey()
        {
            Console.WriteLine("Press any key to continue.");
            while (!Console.KeyAvailable)
            {
                await Task.Delay(1);
            }
            Console.ReadKey(true);
        }

        static async Task RunExample()
        {
            // Let's go back to our embedded connector now, to discuss what the lifetime of a
            // connection looks like.
            //
            // We'll create an embedded connector, but this time we're going to include a maximum
            // ping timeout of 100ms. This is a fairly short timer, but since everything is running
            // in our current process, it should be fine.
            //
            // The ping timer exists in a Buttplug Server as a way to make sure all hardware stops
            // and a disconnection happens in the event of a thread block, remote disconnection, etc.
            // If a maximum ping time is set, then the Buttplug Client is expected to send a Buttplug
            // Ping message to the server with a maximum time gap of the ping time. Luckily,
            // reference Buttplug Clients, like we're using here, will handle that for you.
            var connector = new ButtplugEmbeddedConnector("Example Server", 100);
            var client = new ButtplugNoPingTestClient(connector);

            // That said, just because the Client takes care of sending the message for you doesn't
            // mean that a connection is always perfect. It could be that some code you write blocks
            // the thread that the timer is sending on, or sometimes the client's connection to the
            // server can be severed. In these cases, the client has events we can listen to so we
            // know when either we pinged out, or the server was disconnected.
            client.PingTimeout += (aObj, aEventArgs) => Console.WriteLine("Buttplug ping timeout!");
            client.ServerDisconnect += (aObj, aEventArgs) => Console.WriteLine("Buttplug disconnected!");

            // Let's go ahead and connect.
            await client.ConnectAsync();
            Console.WriteLine("Client connected");
            // If we just sit here and wait, the client and server will happily ping each other
            // internally, so we shouldn't see anything printed outside of the "hit key to continue"
            // message. Our wait function is async, so the event loop still spins and the timer stays happy.
            await WaitForKey();

            // Now we'll kill the timer. You should see both a ping timeout and a disconnected message from
            // the event handlers we set up above.
            Console.WriteLine("Stopping ping timer");
            client.StopPingTimer();
            await WaitForKey();

            // At this point we should already be disconnected, so we'll just show ourselves out.
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
