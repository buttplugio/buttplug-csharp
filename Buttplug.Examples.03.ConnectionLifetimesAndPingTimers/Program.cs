using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Client;

namespace Buttplug.Examples._03.ConnectionLifetimesAndPingTimers
{
    class Program
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

        static async Task RunExample()
        {
            // Let's go back to our embedded connector now, to discuss what the lifetime of a
            // connection looks like.
            //
            // First off, please run this example in a debugger, 'cause we kinda need to be able to
            // pause everything so we'll be using the debugger break command.
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
            var client = new ButtplugClient("Example Client", connector);

            // That said, just because the Client takes care of sending the message for you doesn't
            // mean that a connection is always perfect. It could be that some code you write blocks
            // the thread that the timer is sending on, or sometimes the client's connection to the
            // server can be severed. In these cases, the client has events we can listen to so we
            // know when either we pinged out, or the server was disconnected.
            client.PingTimeout += (aObj, aEventArgs) => Console.WriteLine("Buttplug timeout!");
            client.ServerDisconnect += (aObj, aEventArgs) => Console.WriteLine("Buttplug Disconnected!");

            // Let's go ahead and connect.
            await client.ConnectAsync();

            // If we just sit here and wait, the client and server will happily ping each other
            // internally, so we shouldn't see anything printed outside of the "hit key to continue"
            // message. Our wait function is async, so the event loop still spins and the timer stays happy.
            await WaitForKey();

            // Now we'll cause a debug break, which halts everything. It'll probably take you longer
            // than 100ms to hit the continue button, meaning the ping timer will time out, and when
            // the program continues, you'll see both a ping timeout and a disconnected message from
            // the event handlers we set up above.
            Console.WriteLine("Breaking for Debugger");
            Debugger.Break();
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
