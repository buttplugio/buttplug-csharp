// Buttplug C# - Async Patterns Example
//
// This example demonstrates async/await patterns and event handling
// in the Buttplug C# library. The library is fully async - all operations
// that might block (network, device communication) use async/await.

using Buttplug.Client;

var client = new ButtplugClient("Async Example");

// Events in C# use the standard EventHandler pattern.
// Handlers receive (object sender, EventArgs args).

// DeviceAdded is fired when a new device connects
client.DeviceAdded += async (sender, args) =>
{
    // Note: Event handlers can be async!
    // The device is available via args.Device
    Console.WriteLine($"[Event] Device added: {args.Device.Name}");

    // You can interact with the device in the event handler
    if (args.Device.HasOutput(Buttplug.Core.Messages.OutputType.Vibrate))
    {
        Console.WriteLine($"  Sending welcome vibration...");
        await args.Device.VibrateAsync(0.25);
        await Task.Delay(200);
        await args.Device.StopAsync();
    }
};

// DeviceRemoved is fired when a device disconnects
client.DeviceRemoved += (sender, args) =>
{
    Console.WriteLine($"[Event] Device removed: {args.Device.Name}");
};

// ScanningFinished is fired when scanning completes
// (some protocols scan continuously until stopped)
client.ScanningFinished += (sender, args) =>
{
    Console.WriteLine("[Event] Scanning finished");
};

// ErrorReceived is fired for asynchronous errors
// (errors not directly caused by a method call you awaited)
client.ErrorReceived += (sender, args) =>
{
    Console.WriteLine($"[Event] Error: {args.Exception.Message}");
};

// ServerDisconnect is fired when the server connection drops
client.ServerDisconnect += (sender, args) =>
{
    Console.WriteLine("[Event] Server disconnected!");
};

// PingTimeout is fired if the server doesn't respond to keep-alive pings
client.PingTimeout += (sender, args) =>
{
    Console.WriteLine("[Event] Server ping timeout!");
};

// InputReadingReceived is fired when subscribed sensor data arrives
client.InputReadingReceived += (sender, args) =>
{
    Console.WriteLine($"[Event] Input reading from device {args.DeviceIndex}: {args.Reading}");
};

// Connect asynchronously - this may take time due to network
Console.WriteLine("Connecting to server...");
await client.ConnectAsync("ws://127.0.0.1:12345");
Console.WriteLine("Connected!\n");

// Scanning is also async - we start it and wait for events
Console.WriteLine("Starting scan. Turn on devices now...");
Console.WriteLine("(Events will be printed as devices connect)\n");
await client.StartScanningAsync();

// Use CancellationToken for timeouts and cancellation
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    // Wait for user input or timeout
    Console.WriteLine("Press Enter to stop scanning (or wait 10 seconds)...");
    await Task.Run(() => Console.ReadLine(), cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Scan timeout reached.");
}

await client.StopScanningAsync();

// Demonstrate concurrent operations
Console.WriteLine("\nDemonstrating concurrent device control...");
var devices = client.Devices;
if (devices.Length > 0)
{
    // Send commands to all devices concurrently
    var tasks = devices
        .Where(d => d.HasOutput(Buttplug.Core.Messages.OutputType.Vibrate))
        .Select(async device =>
        {
            Console.WriteLine($"  Vibrating {device.Name}...");
            await device.VibrateAsync(0.5);
            await Task.Delay(500);
            await device.StopAsync();
            Console.WriteLine($"  {device.Name} stopped.");
        });

    // Wait for all commands to complete
    await Task.WhenAll(tasks);
    Console.WriteLine("All devices stopped.");
}
else
{
    Console.WriteLine("No devices connected.");
}

Console.WriteLine("\nPress Enter to disconnect...");
Console.ReadLine();

await client.DisconnectAsync();
Console.WriteLine("Disconnected.");
