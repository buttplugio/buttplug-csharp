// Buttplug C# - Connection Example
//
// This example demonstrates how to connect to a Buttplug server
// (like Intiface Central) and handle connection errors.

using Buttplug.Client;
using Buttplug.Core;

// Create a client with your application's name.
// This name will be shown in Intiface Central.
var client = new ButtplugClient("Connection Example");

try
{
    // Connect to the server. The extension method creates a WebSocket connector
    // automatically from the URI string. Default port for Intiface Central is 12345.
    await client.ConnectAsync("ws://127.0.0.1:12345");
    Console.WriteLine("Connected! Check Intiface Central for the client name.");
    Console.WriteLine("Press Enter to disconnect...");
    Console.ReadLine();

    // Disconnect cleanly
    await client.DisconnectAsync();
}
catch (ButtplugClientConnectorException ex)
{
    // Connection failed - server not running, wrong address, network issues, etc.
    Console.WriteLine($"Can't connect to server: {ex.Message}");
    Console.WriteLine("Make sure Intiface Central is running and the server is started.");
}
catch (ButtplugHandshakeException ex)
{
    // Client/server version mismatch - need to upgrade one or the other
    Console.WriteLine($"Handshake failed: {ex.Message}");
    Console.WriteLine("Client and server versions may be incompatible.");
}
catch (ButtplugException ex)
{
    // Other Buttplug-specific errors
    Console.WriteLine($"Buttplug error: {ex.Message}");
}

Console.WriteLine("Press Enter to exit...");
Console.ReadLine();
