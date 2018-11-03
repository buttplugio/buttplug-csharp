using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Client;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Core.Test;
using Buttplug.Server.Test;

// Tutorial file, disable ConfigureAwait checking since it's an actual program.
// ReSharper disable ConsiderUsingConfigureAwait

namespace Buttplug.Examples._06.LoggingAndErrorHandling
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

        private static async Task RunExample()
        {
            // Now that we've gone through all of the things Buttplug can do, let's cover what
            // happens when you try something it can't do.
            //
            // Using the Buttplug Client API should, in most cases not involving connector setup,
            // look exactly the same. This mean that you should get the same exceptions either using
            // embedded connectors or remote connectors. Your application should "just work"
            // regardless of how client and server are connected, though it may be a bit slower if
            // there's a remote client/server setup.
            //
            // Let's see what accidentally screwing up in Buttplug looks like.
            //
            // First off, all Buttplug specific exceptions derive from ButtplugException, so if you
            // want to catch everything that could come out of the library, that's the base class to
            // start catching with.

            // Let's go ahead, put our client/server together, and get connected.
            var connector = new ButtplugEmbeddedConnector("Example Server");
            var client = new ButtplugClient("Example Client", connector);
            
            // We've set up our client and connector as usual, but we haven't called ConnectAsync()
            // yet. Let's call StartScanningAsync() and see what happens.
            try
            {
                await client.StartScanningAsync();
            }
            catch (ButtplugClientConnectorException ex)
            {
                // Sure enough, we get an exception. In this case, it's a
                // ButtplugClientConnectorException. Buttplug itself doesn't really have an idea of
                // connection states (since it's basically communication agnostic), so we handle
                // connection errors via ButtplugClientConnectorExceptions.
                Console.WriteLine($"Can't scan because we're not connected yet! Message: {ex.Message}");
            }

            // Let's go ahead, set up our test device, and actually connect now.
            var server = connector.Server;
            var testDevice = new TestDevice(new ButtplugLogManager(), "Test Device");
            server.AddDeviceSubtypeManager(
                aLogManager => new TestDeviceSubtypeManager(testDevice));
            await client.ConnectAsync();

            Console.WriteLine("Connected!");

            // Getting errors is certainly helpful, but sometimes it'd also be nice to know what's
            // going on between the errors, right? That's where logging comes in. Let's sign up for
            // some logs.
            //
            // First off, we'll want to assign a Log event handler to the client, to make sure we do
            // something when we get log messages.
            void HandleLogMessage(object aObj, LogEventArgs aArgs)
            {
                Console.WriteLine($"LOG: {aArgs.Message.LogMessage}");
            }

            client.Log += HandleLogMessage;
            // Calling RequestLogAsync tells the server to send us all log messages at the requested
            // level and below. The log levels are:
            //
            // - Off
            // - Fatal
            // - Error
            // - Warn
            // - Info
            // - Debug
            // - Trace
            //
            // If we set to Debug, this means we'll get all messages that aren't Trace.
            await client.RequestLogAsync(ButtplugLogLevel.Debug);

            // Now that we've registered, whenever log messages are generated, our log handler should
            // fire and print them to the console.

            // You usually shouldn't run Start/Stop scanning back-to-back like this, but with
            // TestDevice we know our device will be found when we call StartScanning, so we can get
            // away with it.
            await client.StartScanningAsync();
            await client.StopScanningAsync();
            Console.WriteLine("Client currently knows about these devices:");
            foreach (var device in client.Devices)
            {
                Console.WriteLine($"- {device.Name}");
            }

            await WaitForKey();

            // Thanks to the last tutorials, we know we've got a test device now. Let's send it a
            // weird message it doesn't expect and see what happens.
            try
            {
                await client.Devices[0].SendLovenseCmd("Vibrate:20;");
            }
            catch (ButtplugDeviceException e)
            {
                // Sure enough, it's going to fail again, this time with a Device Exception, because,
                // well, this has to do with a device this time.
                Console.WriteLine(e);
            }

            // There are 5 different types of Buttplug Exceptions:
            //
            // - ButtplugHandshakeException - Something went wrong while the client was connecting to
            //   the server
            // - ButtplugDeviceException - Something went wrong with a device.
            // - ButtplugMessageException - Something went wrong while forming a message.
            // - ButtplugPingException - Client missed pinging the server and a ping timeout happened.
            // - ButtplugUnknownException - Something went wrong. Somewhere. YOLO. (This should
            //   rarely if ever get thrown)
            //
            // All of these exceptions are translations of Buttplug Error messages, meaning if a
            // remote server sends an error message, we can still turn it into an Exception on the
            // Client side, to make it look local.
            await WaitForKey();

            // And that's it! Now you know how Buttplug API error handling works, and how to interact
            // with the internal logging utilities.
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
