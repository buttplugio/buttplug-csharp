// Buttplug C# - Ping Timeout Example
//
// This example shows how to handle the PingTimeout event.
// Note: Ping handling is automatic in the C# client. The PingTimeout
// event fires when the server fails to respond, indicating the connection
// should be considered dead.

using Buttplug.Client;

var client = new ButtplugClient("Ping Example");

// The PingTimeout event fires when the server doesn't respond to keep-alive pings.
// This usually means the connection has been lost.
client.PingTimeout += (sender, args) =>
{
    Console.WriteLine("Ping timeout! Server connection lost.");
    Console.WriteLine("All devices should be stopped by the server.");
    // In a real application, you would:
    // - Update UI to show disconnected state
    // - Attempt to reconnect if appropriate
    // - Clean up any resources
};

// Connect normally - ping handling is automatic
await client.ConnectAsync("ws://127.0.0.1:12345");
Console.WriteLine("Connected. Ping keep-alive is handled automatically.");

Console.WriteLine("Press Enter to disconnect...");
Console.ReadLine();

await client.DisconnectAsync();
