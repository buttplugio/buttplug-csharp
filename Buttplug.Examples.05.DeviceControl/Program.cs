using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Client;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Core.Test;
using Buttplug.Server.Test;

namespace Buttplug.Examples._05.DeviceControl
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
            // Finally! It's time to make something move!
            //
            // (In this case, that "something" will just be a Test Device, so this is actually just a
            // simulation of something moving. Sorry to get you all excited.)

            // Let's go ahead, put our client/server together, and get connected.
            var connector = new ButtplugEmbeddedConnector("Example Server");
            var client = new ButtplugClient("Example Client", connector);
            var server = connector.Server;
            var testDevice = new TestDevice(new ButtplugLogManager(), "Test Device");
            server.AddDeviceSubtypeManager(
                (IButtplugLogManager aLogManager) => new TestDeviceSubtypeManager(testDevice));
            try
            {
                await client.ConnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Can't connect to Buttplug Server, exiting! Message: {ex.InnerException.Message}");
                await WaitForKey();
                return;
            }
            Console.WriteLine("Connected!");
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

            // Ok, so we now have a connected client with a device set up. Let's start sending some
            // messages to make the device do things!
            //
            // It's worth noting that at the moment, a client knowing about a device is enough to
            // assume that device is connected to the server and ready to use. So if a client has a
            // device in its list, we can just start sending control messages.
            //
            // We'll need to see which messages our device handles. Luckily, devices hold this
            // information for you to query.
            //
            // When building applications, we can use AllowedMessages to see what types of messages
            // whatever device handed to us can take, and then react accordingly.

            foreach (var device in client.Devices)
            {
                Console.WriteLine($"{device.Name} supports the following messages:");
                foreach (var msgInfo in device.AllowedMessages)
                {
                    // msgInfo will have two pieces of information
                    // - Message name, which should be the same as the message type.
                    // - Message constraints, which can vary depending on the type of message
                    //
                    // For instance the VibrateCmd message will have a "name" of VibrateCmd, and a
                    // "FeatureCount" of 1 < x < N, depending on the number of vibration motors the
                    // device has. Messages that don't have a FeatureCount will leave FeatureCount as null.
                    //
                    // Since we're working with a TestDevice, we know it will support 3 different
                    // types of messages.
                    //
                    // - VibrateCmd with a FeatureCount of 2, meaning we can send 2 vibration
                    // commands at a time.
                    // - SingleMotorVibrateCmd, a legacy message that makes all vibrators on a device
                    // vibrate at the same speed.
                    // - StopDeviceCmd, which stops all output on a device. All devices should
                    // support this message.
                    Console.WriteLine($"- {msgInfo.Key}");
                    if (msgInfo.Value.FeatureCount != null)
                    {
                        Console.WriteLine($"  - Feature Count: {msgInfo.Value.FeatureCount}");
                    }
                }
            }

            Console.WriteLine("Sending commands");

            // Now that we know the message types for our connected device, we can send a message
            // over! Seeing as we want to stick with the modern generic messages, we'll go with VibrateCmd.
            //
            // There's a couple of ways to send this message.
            var testClientDevice = client.Devices[0];
            var vibratorCount = testClientDevice.AllowedMessages["VibrateCmd"].FeatureCount;

            // We can create the message manually and send it over through the device object.
            var vibrateCmdMsg = new VibrateCmd(new List<VibrateCmd.VibrateSubcommand> { new VibrateCmd.VibrateSubcommand(0, 1.0) });
            await testClientDevice.SendMessageAsync(vibrateCmdMsg);

            // We can also use the .Create() function on Generic messages to make life a bit easier.
            // For instance, with VibrateCmd, the create function just makes a VibrateCmd message for
            // us and sets all vibration motors to the same speed in that message.
            var createVibrateCmdMsg = VibrateCmd.Create(1.0, vibratorCount ?? 0);
            await testClientDevice.SendMessageAsync(createVibrateCmdMsg);

            await WaitForKey();

            // And now we disconnect as usual.
            await client.DisconnectAsync();

            // If we try to send a command to a device after the client has disconnected, we'll get
            // an exception thrown.
            try
            {
                await testClientDevice.SendMessageAsync(createVibrateCmdMsg);
            }
            catch (Exception e)
            {
                Console.WriteLine("Tried to send a device message after the client disconnected! Exception: ");
                Console.WriteLine(e);
                throw;
            }
            await WaitForKey();
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
