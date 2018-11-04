using Buttplug.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buttplug.Client.Connectors.WebsocketConnector;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

// Tutorial file, disable ConfigureAwait checking since it's an actual program.
// ReSharper disable ConsiderUsingConfigureAwait

namespace Buttplug.Examples._07.FullProgram
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
            // Now that we've seen all of the different parts of Buttplug, let's put them together in
            // a small program.
            //
            // This program will:
            // - Create an embedded (or possibly websocket) connector
            // - Scan, this time using real Managers, so we'll see devices (assuming you have them
            // hooked up)
            // - List the connected devices for the user
            // - Let the user select a device, and trigger some sort of event on that device
            // (vibration, thrusting, etc...).

            // As usual, we start off with our connector setup. We really don't need access to the
            // connector this time, so we can just pass the created connector directly to the client.
            var client = new ButtplugClient("Example Client", new ButtplugEmbeddedConnector("Example Server"));
            
            // If you want to use a websocket client and talk to a websocket server instead,
            // uncomment the following line and comment the one above out. Note you will need to turn
            // off TLS/SSL on the server.

            // var client = new ButtplugClient("Example Client", new ButtplugWebsocketConnector(new Uri("ws://localhost:12345/buttplug")));

            await client.ConnectAsync();

            // At this point, if you want to see everything that's happening, uncomment this block to
            // turn on logging. Warning, it might be pretty spammy.

            void HandleLogMessage(object aObj, LogEventArgs aArgs) { Console.WriteLine($"LOG: {aArgs.Message.LogMessage}"); }
            client.Log += HandleLogMessage; await client.RequestLogAsync(ButtplugLogLevel.Debug);

            // Now we scan for devices. Since we didn't add any Subtype Managers yet, this will go
            // out and find them for us. They'll be reported in the logs as they are found.
            //
            // We'll scan for devices, and print any time we find one.
            void HandleDeviceAdded(object aObj, DeviceAddedEventArgs aArgs)
            {
                Console.WriteLine($"Device connected: {aArgs.Device.Name}");
            }

            client.DeviceAdded += HandleDeviceAdded;

            async Task ScanForDevices()
            {
                Console.WriteLine("Scanning for devices until key is pressed.");
                await client.StartScanningAsync();
                await WaitForKey();

                // Stop scanning now, 'cause we don't want new devices popping up anymore.
                await client.StopScanningAsync();
            }

            // Scan for devices before we get to our menu.
            await ScanForDevices();

            async Task ControlDevice()
            {
                List<uint> options = new List<uint>();
                // Controlling a device has 2 steps: selecting the device to control, and choosing
                // which command to send. We'll just list the devices the client has available, then
                // search the device message capabilities once that's done to figure out what we can send.
                foreach (var dev in client.Devices)
                {
                    Console.WriteLine($"{dev.Index}. {dev.Name}");
                    options.Add(dev.Index);
                }
                Console.WriteLine("Choose a device: ");
                if (!uint.TryParse(Console.ReadLine(), out var deviceChoice) || !options.Contains(deviceChoice))
                {
                    Console.WriteLine("Invalid choice");
                    return;
                }

                var device = client.Devices.First(dev => dev.Index == deviceChoice);
                var commandTypes = device.AllowedMessages.Keys.Intersect(new[] { typeof(VibrateCmd), typeof(RotateCmd), typeof(LinearCmd) }).ToArray();

                Console.WriteLine("Choose an action:");
                uint i = 1;
                foreach (var command in commandTypes)
                {
                    Console.WriteLine($"{i}. {command.Name.Substring(0, command.Name.Length - 3)}");
                    ++i;
                }
                if (!uint.TryParse(Console.ReadLine(), out var cmdChoice) ||
                    cmdChoice - 1 > commandTypes.Length)
                {
                    Console.WriteLine("Invalid choice, try again.");
                    return;
                }

                var cmdType = commandTypes[cmdChoice - 1];

                // Pattern matching for switch blocks doesn't seem to work here. :(
                if (cmdType == typeof(VibrateCmd))
                {
                    Console.WriteLine($"Vibrating all motors of {device.Name} at 50% for 1s.");
                    try
                    {
                        await device.SendVibrateCmd(0.5);
                        await Task.Delay(1000);
                        await device.SendVibrateCmd(0);
                    }
                    catch (ButtplugDeviceException)
                    {
                        Console.WriteLine("Device disconnected. Please try another device.");
                    }
                }
                else if (cmdType == typeof(RotateCmd))
                {
                    Console.WriteLine($"Rotating {device.Name} at 50% for 1s.");
                    try
                    {
                        await device.SendRotateCmd(0.5, true);
                        await Task.Delay(1000);
                        await device.SendRotateCmd(0, true);
                    }
                    catch (ButtplugDeviceException)
                    {
                        Console.WriteLine("Device disconnected. Please try another device.");
                    }
                }
                else if (cmdType == typeof(LinearCmd))
                {
                    Console.WriteLine($"Oscillating linear motors of {device.Name} from 20% to 80% over 3s");
                    try
                    {
                        await device.SendLinearCmd(1000, 0.2);
                        await Task.Delay(1100);
                        await device.SendLinearCmd(1000, 0.8);
                        await Task.Delay(1100);
                        await device.SendLinearCmd(1000, 0.2);
                        await Task.Delay(1100);
                    }
                    catch (ButtplugDeviceException)
                    {
                        Console.WriteLine("Device disconnected. Please try another device.");
                    }
                }
            }

            while (true)
            {
                Console.WriteLine("1. Scan For More Devices\n2. Control Devices\n3. Quit\nChoose an option: ");
                if (!uint.TryParse(Console.ReadLine(), out var choice) ||
                    (choice == 0 || choice > 3))
                {
                    Console.WriteLine("Invalid choice, try again.");
                    continue;
                }

                switch (choice)
                {
                    case 1:
                        await ScanForDevices();
                        continue;
                    case 2:
                        await ControlDevice();
                        continue;
                    case 3:
                        return;
                    default:
                        // Due to the check above, we'll never hit this, but eh.
                        continue;
                }
            }
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