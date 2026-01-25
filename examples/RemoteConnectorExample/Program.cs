// Buttplug C# - Remote Connector Example
//
// This example demonstrates the explicit WebSocket connector setup.
// While the ConnectAsync(string) extension method is convenient,
// creating the connector explicitly gives you more control.

using Buttplug.Client;

// Method 1: Using the convenience extension (recommended for most cases)
// This creates a WebSocket connector automatically from a URI string.
var client1 = new ButtplugClient("Simple Connection");
Console.WriteLine("Method 1: ConnectAsync(string)");
Console.WriteLine("  await client.ConnectAsync(\"ws://127.0.0.1:12345\");");
Console.WriteLine("  (Creates WebSocket connector automatically)\n");

// Method 2: Using a Uri object
// Still uses the extension method, but allows Uri manipulation first.
var client2 = new ButtplugClient("Uri Connection");
var uri = new Uri("ws://127.0.0.1:12345");
Console.WriteLine("Method 2: ConnectAsync(Uri)");
Console.WriteLine($"  var uri = new Uri(\"{uri}\");");
Console.WriteLine("  await client.ConnectAsync(uri);");
Console.WriteLine("  (Useful when building URIs programmatically)\n");

// Method 3: Explicit connector creation
// Use this when you need custom connector configuration or
// when implementing a custom connector.
var client3 = new ButtplugClient("Explicit Connector");
var connector = new ButtplugWebsocketConnector(new Uri("ws://127.0.0.1:12345"));
Console.WriteLine("Method 3: Explicit ButtplugWebsocketConnector");
Console.WriteLine("  var connector = new ButtplugWebsocketConnector(uri);");
Console.WriteLine("  await client.ConnectAsync(connector);");
Console.WriteLine("  (Most control, required for custom connectors)\n");

// Let's actually connect using the explicit connector method
Console.WriteLine("Connecting using explicit connector...");
try
{
    await client3.ConnectAsync(connector);
    Console.WriteLine("Connected successfully!");
    Console.WriteLine($"  Client name: {client3.Name}");
    Console.WriteLine($"  Connected: {client3.Connected}");

    Console.WriteLine("\nPress Enter to disconnect...");
    Console.ReadLine();

    await client3.DisconnectAsync();
    Console.WriteLine("Disconnected.");
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
    Console.WriteLine("\nMake sure Intiface Central is running with the server started.");
}

Console.WriteLine("\nPress Enter to exit...");
Console.ReadLine();
