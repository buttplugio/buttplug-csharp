// Buttplug C# - Device Control Example
//
// This example demonstrates how to send commands to devices,
// query device capabilities, and use the command builder API.

using Buttplug.Client;
using Buttplug.Core.Messages;

var client = new ButtplugClient("Device Control Example");

// Connect and scan for devices
Console.WriteLine("Connecting...");
await client.ConnectAsync("ws://127.0.0.1:12345");
Console.WriteLine("Connected! Scanning for devices...");

await client.StartScanningAsync();
Console.WriteLine("Turn on a device, then press Enter...");
Console.ReadLine();
await client.StopScanningAsync();

// Check if we have any devices
if (client.Devices.Length == 0)
{
    Console.WriteLine("No devices found. Exiting.");
    await client.DisconnectAsync();
    return;
}

// Get the first device
var device = client.Devices[0];
Console.WriteLine($"\nUsing device: {device.Name}");

// Show what output types this device supports
Console.WriteLine("\nSupported output types:");
foreach (var feature in device.Features.Values)
{
    var outputs = feature.FeatureDefinition.Output;
    if (outputs != null)
    {
        foreach (var outputType in outputs)
        {
            Console.WriteLine($"  - {outputType} (Feature {feature.FeatureIndex}: {feature.FeatureDescriptor})");
        }
    }
}

// Check for vibration support and demonstrate commands
if (device.HasOutput(OutputType.Vibrate))
{
    var vibrateFeatures = device.GetFeaturesWithOutput(OutputType.Vibrate).ToList();
    Console.WriteLine($"\nDevice has {vibrateFeatures.Count} vibrator(s).");

    // Method 1: Use the convenience extension method
    Console.WriteLine("\nVibrating at 50% using convenience method...");
    await device.VibrateAsync(0.5);
    await Task.Delay(1000);

    // Method 2: Use the command builder API for more control
    Console.WriteLine("Vibrating at 75% using command builder...");
    await device.RunOutputAsync(DeviceOutput.Vibrate.Percent(0.75));
    await Task.Delay(1000);

    // Method 3: Send command to a specific feature
    if (vibrateFeatures.Count > 0)
    {
        var firstVibrator = vibrateFeatures[0];
        Console.WriteLine($"Vibrating feature '{firstVibrator.FeatureDescriptor}' at 25%...");
        await device.RunOutputAsync(firstVibrator.FeatureIndex, DeviceOutput.Vibrate.Percent(0.25));
        await Task.Delay(1000);
    }

    // Stop the device
    Console.WriteLine("Stopping device...");
    await device.StopAsync();
}
else
{
    Console.WriteLine("\nDevice does not support vibration.");
}

// Demonstrate other output types if available
if (device.HasOutput(OutputType.Rotate))
{
    Console.WriteLine("\nDevice supports rotation. Rotating at 50%...");
    await device.RotateAsync(0.5);
    await Task.Delay(1000);
    await device.StopAsync();
}

if (device.HasOutput(OutputType.Position))
{
    Console.WriteLine("\nDevice supports position control. Moving to 100% over 500ms...");
    await device.PositionWithDurationAsync(1.0, 500);
    await Task.Delay(1000);
    await device.PositionWithDurationAsync(0.0, 500);
}

// Try reading battery level if supported
if (device.HasInput(InputType.Battery))
{
    Console.WriteLine("\nReading battery level...");
    var battery = await device.BatteryAsync();
    Console.WriteLine($"Battery: {battery * 100:F0}%");
}

Console.WriteLine("\nPress Enter to disconnect...");
Console.ReadLine();

// Disconnect - this automatically stops all devices
await client.DisconnectAsync();
Console.WriteLine("Disconnected.");
