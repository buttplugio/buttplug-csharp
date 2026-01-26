// Buttplug C# - Device Enumeration Example
//
// This example demonstrates how to scan for devices and handle
// device connection/disconnection events.

using Buttplug.Client;

var client = new ButtplugClient("Device Enumeration Example");

// Set up event handlers BEFORE connecting.
// This ensures we don't miss any events.

client.DeviceAdded += (sender, args) =>
{
    Console.WriteLine($"Device connected: {args.Device.Name}");
};

client.DeviceRemoved += (sender, args) =>
{
    Console.WriteLine($"Device disconnected: {args.Device.Name}");
};

client.ScanningFinished += (sender, args) =>
{
    Console.WriteLine("Scanning finished.");
};

// Connect to the server
await client.ConnectAsync("ws://127.0.0.1:12345");

// Start scanning for devices.
// Devices will be announced via the DeviceAdded event.
Console.WriteLine("Turn on your devices now!");
await client.StartScanningAsync();

Console.WriteLine("\nPress Enter to stop scanning...");
Console.ReadLine();

// Stop scanning. Some protocols scan continuously until told to stop.
await client.StopScanningAsync();

// The client maintains a list of all known devices.
// This list persists even after scanning stops.
Console.WriteLine("\nCurrently connected devices:");
foreach (var device in client.Devices)
{
    Console.WriteLine($"  - {device.Name} (Index: {device.Index})");
}

if (client.Devices.Length == 0)
{
    Console.WriteLine("  (no devices connected)");
}

Console.WriteLine("\nPress Enter to disconnect...");
Console.ReadLine();

await client.DisconnectAsync();
Console.WriteLine("Disconnected.");
