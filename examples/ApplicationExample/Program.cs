// Buttplug C# - Complete Application Example
//
// This is a complete, working example that demonstrates the full workflow
// of a Buttplug application. If you're new to Buttplug, start here!
//
// Prerequisites:
// 1. Install Intiface Central: https://intiface.com/central
// 2. Start the server in Intiface Central (click "Start Server")
// 3. Run this example

using Buttplug.Client;
using Buttplug.Core;
using Buttplug.Core.Messages;

Console.WriteLine("===========================================");
Console.WriteLine("  Buttplug C# Application Example");
Console.WriteLine("===========================================\n");

// Step 1: Create a client
// The client name identifies your application to the server.
var client = new ButtplugClient("My Buttplug Application");

// Step 2: Set up event handlers
// Always do this BEFORE connecting to avoid missing events.
client.DeviceAdded += (_, args) =>
    Console.WriteLine($"[+] Device connected: {args.Device.Name}");

client.DeviceRemoved += (_, args) =>
    Console.WriteLine($"[-] Device disconnected: {args.Device.Name}");

client.ServerDisconnect += (_, _) =>
    Console.WriteLine("[!] Server connection lost!");

client.ErrorReceived += (_, args) =>
    Console.WriteLine($"[!] Error: {args.Exception.Message}");

// Step 3: Connect to the server
Console.WriteLine("Connecting to Intiface Central...");
try
{
    await client.ConnectAsync("ws://127.0.0.1:12345");
}
catch (ButtplugClientConnectorException)
{
    Console.WriteLine("ERROR: Could not connect to Intiface Central!");
    Console.WriteLine("Make sure Intiface Central is running and the server is started.");
    Console.WriteLine("Default address: ws://127.0.0.1:12345");
    return;
}
Console.WriteLine("Connected!\n");

// Step 4: Scan for devices
Console.WriteLine("Scanning for devices...");
Console.WriteLine("Turn on your Bluetooth/USB devices now.\n");
await client.StartScanningAsync();

// Wait for devices (in a real app, you might use a UI or timeout)
Console.WriteLine("Press Enter when your devices are connected...");
Console.ReadLine();
await client.StopScanningAsync();

// Step 5: Check what devices we found
var devices = client.Devices;
if (devices.Length == 0)
{
    Console.WriteLine("No devices found. Make sure your device is:");
    Console.WriteLine("  - Turned on");
    Console.WriteLine("  - In pairing/discoverable mode");
    Console.WriteLine("  - Supported by Buttplug (check https://iostindex.com)");
    await client.DisconnectAsync();
    return;
}

Console.WriteLine($"\nFound {devices.Length} device(s):\n");

// Step 6: Display device capabilities
foreach (var device in devices)
{
    Console.WriteLine($"  {device.Name}");

    // Check output capabilities (things we can make the device do)
    var outputs = new List<string>();
    if (device.HasOutput(OutputType.Vibrate)) outputs.Add("Vibrate");
    if (device.HasOutput(OutputType.Rotate)) outputs.Add("Rotate");
    if (device.HasOutput(OutputType.Oscillate)) outputs.Add("Oscillate");
    if (device.HasOutput(OutputType.Position)) outputs.Add("Position");
    if (device.HasOutput(OutputType.Constrict)) outputs.Add("Constrict");

    if (outputs.Count > 0)
        Console.WriteLine($"    Outputs: {string.Join(", ", outputs)}");

    // Check input capabilities (sensors we can read)
    var inputs = new List<string>();
    if (device.HasInput(InputType.Battery)) inputs.Add("Battery");
    if (device.HasInput(InputType.RSSI)) inputs.Add("RSSI");
    if (device.HasInput(InputType.Button)) inputs.Add("Button");
    if (device.HasInput(InputType.Pressure)) inputs.Add("Pressure");

    if (inputs.Count > 0)
        Console.WriteLine($"    Inputs: {string.Join(", ", inputs)}");

    Console.WriteLine();
}

// Step 7: Interactive device control
Console.WriteLine("=== Interactive Control ===");
Console.WriteLine("Commands:");
Console.WriteLine("  v <0-100>  - Vibrate all devices at percentage");
Console.WriteLine("  s          - Stop all devices");
Console.WriteLine("  b          - Read battery levels");
Console.WriteLine("  q          - Quit\n");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim().ToLower();

    if (string.IsNullOrEmpty(input)) continue;

    try
    {
        if (input.StartsWith("v "))
        {
            // Vibrate command
            if (int.TryParse(input[2..], out var percent) && percent >= 0 && percent <= 100)
            {
                var intensity = percent / 100.0;
                foreach (var device in devices)
                {
                    if (device.HasOutput(OutputType.Vibrate))
                    {
                        await device.RunOutputAsync(DeviceOutput.Vibrate.Percent(intensity));
                        Console.WriteLine($"  {device.Name}: vibrating at {percent}%");
                    }
                }
            }
            else
            {
                Console.WriteLine("  Usage: v <0-100>");
            }
        }
        else if (input == "s")
        {
            // Stop all devices
            await client.StopAllDevicesAsync();
            Console.WriteLine("  All devices stopped.");
        }
        else if (input == "b")
        {
            // Read battery levels
            foreach (var device in devices)
            {
                if (device.HasInput(InputType.Battery))
                {
                    var battery = await device.BatteryAsync();
                    Console.WriteLine($"  {device.Name}: {battery * 100:F0}% battery");
                }
                else
                {
                    Console.WriteLine($"  {device.Name}: no battery sensor");
                }
            }
        }
        else if (input == "q")
        {
            break;
        }
        else
        {
            Console.WriteLine("  Unknown command. Use v, s, b, or q.");
        }
    }
    catch (ButtplugDeviceException ex)
    {
        Console.WriteLine($"  Device error: {ex.Message}");
    }
    catch (ButtplugException ex)
    {
        Console.WriteLine($"  Error: {ex.Message}");
    }
}

// Step 8: Clean up
Console.WriteLine("\nStopping devices and disconnecting...");
await client.StopAllDevicesAsync();
await client.DisconnectAsync();
Console.WriteLine("Goodbye!");
