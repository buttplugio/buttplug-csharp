// Buttplug C# - Exception Handling Example
//
// This example demonstrates the different exception types in Buttplug
// and how to handle them. This is a reference for error handling patterns.

using Buttplug.Client;
using Buttplug.Core;

// All Buttplug exceptions inherit from ButtplugException.
// Here's the hierarchy:
//
// ButtplugException (base class)
// ├── ButtplugClientConnectorException - Connection/transport issues
// ├── ButtplugHandshakeException       - Client/server version mismatch
// ├── ButtplugDeviceException          - Device communication errors
// ├── ButtplugMessageException         - Invalid message format/content
// └── ButtplugPingException            - Server ping timeout

void HandleButtplugException(ButtplugException ex)
{
    // Pattern match on the specific exception type
    switch (ex)
    {
        case ButtplugClientConnectorException connEx:
            // The connector couldn't establish or maintain connection.
            // Causes: server not running, wrong address, network issues,
            // SSL/TLS problems, connection dropped.
            Console.WriteLine($"[Connector Error] {connEx.Message}");
            Console.WriteLine("Check that the server is running and accessible.");
            break;

        case ButtplugHandshakeException hsEx:
            // Client and server couldn't agree on protocol version.
            // Usually means you need to upgrade client or server.
            Console.WriteLine($"[Handshake Error] {hsEx.Message}");
            Console.WriteLine("Client and server versions may be incompatible.");
            break;

        case ButtplugDeviceException devEx:
            // Something went wrong communicating with a device.
            // Causes: device disconnected, invalid command for device,
            // device rejected command, hardware error.
            Console.WriteLine($"[Device Error] {devEx.Message}");
            Console.WriteLine("The device may have disconnected or doesn't support this command.");
            break;

        case ButtplugMessageException msgEx:
            // The message sent was invalid.
            // Causes: malformed message, missing required fields,
            // invalid parameter values.
            Console.WriteLine($"[Message Error] {msgEx.Message}");
            Console.WriteLine("This usually indicates a bug in the client library or application.");
            break;

        case ButtplugPingException pingEx:
            // Server didn't receive ping in time, connection terminated.
            // The ping system ensures dead connections are detected.
            Console.WriteLine($"[Ping Error] {pingEx.Message}");
            Console.WriteLine("Connection was lost due to ping timeout.");
            break;

        default:
            // Unknown or future exception type
            Console.WriteLine($"[Buttplug Error] {ex.Message}");
            break;
    }
}

// Demonstrate catching exceptions during connection
var client = new ButtplugClient("Exception Example");

Console.WriteLine("Exception Handling Example");
Console.WriteLine("==========================\n");

// Example 1: Connection error (server not running)
Console.WriteLine("1. Attempting to connect to non-existent server...");
try
{
    await client.ConnectAsync("ws://127.0.0.1:99999");
}
catch (ButtplugException ex)
{
    HandleButtplugException(ex);
}

// Example 2: Using the ErrorReceived event for async errors
Console.WriteLine("\n2. Setting up error event handler...");
client.ErrorReceived += (sender, args) =>
{
    Console.WriteLine($"[Async Error Event] {args.Exception.Message}");
    HandleButtplugException(args.Exception);
};

// Example 3: Handling errors when sending commands after disconnect
Console.WriteLine("\n3. Demonstrating error after disconnect...");
try
{
    // Try to connect to actual server this time
    await client.ConnectAsync("ws://127.0.0.1:12345");
    Console.WriteLine("Connected successfully.");

    // Scan briefly to get a device
    await client.StartScanningAsync();
    await Task.Delay(1000);
    await client.StopScanningAsync();

    if (client.Devices.Length > 0)
    {
        var device = client.Devices[0];
        Console.WriteLine($"Found device: {device.Name}");

        // Disconnect
        await client.DisconnectAsync();
        Console.WriteLine("Disconnected.");

        // Now try to send a command - this will throw
        Console.WriteLine("Attempting to send command after disconnect...");
        await device.RunOutputAsync(DeviceOutput.Vibrate.Percent(0.5));
    }
    else
    {
        Console.WriteLine("No devices found to test with.");
        await client.DisconnectAsync();
    }
}
catch (ButtplugException ex)
{
    HandleButtplugException(ex);
}

Console.WriteLine("\nPress Enter to exit...");
Console.ReadLine();
