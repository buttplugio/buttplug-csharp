using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Buttplug.Client;
using Buttplug.Core.Logging;
using Buttplug.Devices.Configuration;
using Buttplug.Server.Connectors.WebsocketServer;
using Buttplug.Server.Connectors;
using Google.Protobuf;

namespace Buttplug.Server.CLI
{
    class ServerCLI
    {
        private NamedPipeClientStream _guiPipe;
        public bool ServerReady { get; }
        private DeviceManager _deviceManager;

        // Simple server that exposes device manager, since we'll need to chain device managers
        // through it for this. This is required because Windows 10 has problems disconnecting from
        // BLE devices without completely stopping and restarting processes. :(
        class CLIServer : ButtplugServer
        {
            public DeviceManager DeviceManager => _deviceManager;
            public IButtplugLogManager LogManager => BpLogManager;

            public CLIServer(string aServerName, uint aMaxPingTime, DeviceManager aDevMgr)
            : base(aServerName, aMaxPingTime, aDevMgr)
            {
            }
        }


        public ServerCLI()
        {

        }

        private async Task SendProcessMessage(ServerProcessMessage aMsg)
        {
            if (_guiPipe == null)
            {
                return;
            }
            var arr = aMsg.ToByteArray();
            await _guiPipe.WriteAsync(aMsg.ToByteArray(), 0, arr.Length);
        }

        private async Task PrintProcessLog(string aLogMsg)
        {
            if (_guiPipe != null)
            {
                var msg = new ServerProcessMessage { ProcessLog = new ServerProcessMessage.Types.ProcessLog { Message = aLogMsg } };
                await SendProcessMessage(msg);
            }
            else
            {
                Console.WriteLine(aLogMsg);
            }
        }

        public void RunServer(Options aOptions)
        {
            if (aOptions.Version)
            {
                Console.WriteLine(int.Parse(ThisAssembly.Git.Commits) == 0
                    ? ThisAssembly.Git.BaseTag
                    : ThisAssembly.Git.Tag);
                return;
            }

            if (aOptions.GuiPipe != null)
            {
                // Set up IPC Pipe and wait for connection before continuing, so any errors will show
                // up in the GUI.
                _guiPipe =
                    new NamedPipeClientStream(".", aOptions.GuiPipe, PipeDirection.InOut);
                try
                {
                    _guiPipe.Connect(100);
                }
                catch(TimeoutException)
                {
                    Console.WriteLine("Can't connect to GUI pipe! Exiting.");
                    return;
                }

                SendProcessMessage(new ServerProcessMessage { ProcessStarted = new ServerProcessMessage.Types.ProcessStarted() } ).Wait();
            }

            if (aOptions.GenerateCertificate)
            {
                // CertUtils.GenerateSelfSignedCert(aOptions.CertFile, aOptions.PrivFile);
                Console.WriteLine("Cannot currently generate certificates.");
                return;
            }

            if (aOptions.DeviceConfigFile != null)
            {
                DeviceConfigurationManager.LoadBaseConfigurationFile(aOptions.DeviceConfigFile);
            }
            else
            {
                DeviceConfigurationManager.LoadBaseConfigurationFromResource();
            }

            if (aOptions.UserDeviceConfigFile != null)
            {
                DeviceConfigurationManager.Manager.LoadUserConfigurationFile(aOptions.UserDeviceConfigFile);
            }

            if (aOptions.UseWebsocketServer && aOptions.UseIpcServer)
            {
                PrintProcessLog("ERROR: Can't run websocket server and IPC server at the same time!").Wait();
                return;
            }

            if (!aOptions.UseWebsocketServer && !aOptions.UseIpcServer)
            {
                PrintProcessLog("ERROR: Must specify either IPC server or Websocket server!").Wait();
                return;
            }

            var logLevel = ButtplugLogLevel.Off;
            if (aOptions.Log != null)
            {
                if (!Enum.TryParse(aOptions.Log, out logLevel))
                {
                    PrintProcessLog("ERROR: Invalid log level!").Wait();
                    return;
                }
            }

            ButtplugServer ServerFactory()
            {
                var server = new CLIServer(aOptions.ServerName, (uint)aOptions.PingTime, _deviceManager);
                if (_deviceManager == null)
                {
                    _deviceManager = server.DeviceManager;
                }

                if (logLevel != ButtplugLogLevel.Off)
                {
                    server.LogManager.AddLogListener(logLevel, async (aLogMsg) =>
                    {
                        await PrintProcessLog(aLogMsg.LogMessage);
                    });
                }

                return server;
            }

            var ipcServer = new ButtplugIPCServer();
            var insecureWebsocketServer = new ButtplugWebsocketServer();
            var secureWebsocketServer = new ButtplugWebsocketServer();
            var wait = new TaskCompletionSource<bool>();
            if (aOptions.UseWebsocketServer)
            {
                if (aOptions.WebsocketServerInsecurePort != 0)
                {
                    insecureWebsocketServer.StartServerAsync(ServerFactory, 1, aOptions.WebsocketServerInsecurePort, !aOptions.WebsocketServerAllInterfaces).Wait();
                    insecureWebsocketServer.ConnectionClosed += (aSender, aArgs) => { wait.SetResult(true); };
                    PrintProcessLog("Insecure websocket Server now running...").Wait();
                }
                if (aOptions.WebsocketServerSecurePort != 0 && aOptions.CertFile != null && aOptions.PrivFile != null)
                {
                    secureWebsocketServer.StartServerAsync(ServerFactory, 1, aOptions.WebsocketServerSecurePort, !aOptions.WebsocketServerAllInterfaces, aOptions.CertFile, aOptions.PrivFile).Wait();
                    secureWebsocketServer.ConnectionClosed += (aSender, aArgs) => { wait.SetResult(true); };
                    PrintProcessLog("Secure websocket Server now running...").Wait();
                }
            }
            else if (aOptions.UseIpcServer)
            {
                ipcServer.StartServer(ServerFactory, aOptions.IpcPipe);
                ipcServer.ConnectionClosed += (aSender, aArgs) => { wait.SetResult(true); };
                PrintProcessLog("IPC Server now running...").Wait();
            }
            wait.Task.Wait();
        }
    }
}
