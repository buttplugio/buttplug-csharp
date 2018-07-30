using System;
using Buttplug.Server;
using Buttplug.Server.Managers.ETSerialManager;
using Buttplug.Server.Managers.HidManager;
using Buttplug.Server.Managers.UWPBluetoothManager;
using Buttplug.Server.Managers.WinUSBManager;
using Buttplug.Server.Managers.XInputGamepadManager;
using Microsoft.Win32;

namespace Buttplug.Apps.ServerCLI
{
    class BPServerFactory : IButtplugServerFactory
    {
        private DeviceManager _deviceManager;
        private int _releaseId;

        public BPServerFactory()
        {
            try
            {
                _releaseId = int.Parse(Registry
                    .GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", string.Empty)
                    .ToString());
                Console.WriteLine($"Windows Release ID: {_releaseId}");
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot retreive Release ID for OS! Will not load bluetooth manager.");
            }

            if (!UWPBluetoothManager.HasRegistryKeysSet())
            {
                Console.WriteLine("Registry keys not set for UWP bluetooth API security. This may cause Bluetooth devices to not be seen.");
            }
        }

        private ButtplugServer InitializeButtplugServer(string aServerName, uint aMaxPingTime)
        {
            // Set up internal services
            ButtplugServer bpServer;

            // Due to the weird inability to close BLE devices, we have to share device managers
            // across buttplug server instances. Otherwise we'll just hold device connections open forever.
            if (_deviceManager == null)
            {
                bpServer = new ButtplugServer(aServerName, aMaxPingTime);
                _deviceManager = bpServer.DeviceManager;
            }
            else
            {
                bpServer = new ButtplugServer(aServerName, aMaxPingTime, _deviceManager);
                return bpServer;
            }

            if (!(Environment.OSVersion is null))
            {
                Console.WriteLine($"Windows Version: {Environment.OSVersion.VersionString}");
            }
            else
            {
                Console.WriteLine("Cannot retreive Environment.OSVersion string.");
            }

            // Make sure we're on the Creators update before even trying to load the UWP Bluetooth Manager
            if (_releaseId >= 1703)
            {
                try
                {
                    bpServer.AddDeviceSubtypeManager(aLogger => new UWPBluetoothManager(aLogger));
                }
                catch (PlatformNotSupportedException e)
                {
                    Console.WriteLine($"Something went wrong while setting up bluetooth. {e}");
                }
            }
            else
            {
                Console.WriteLine("OS Version too old to load bluetooth core. Must be Windows 10 15063 or higher.");
            }

            bpServer.AddDeviceSubtypeManager(aLogger => new XInputGamepadManager(aLogger));
            bpServer.AddDeviceSubtypeManager(aLogger => new ETSerialManager(aLogger));
            bpServer.AddDeviceSubtypeManager(aLogger => new WinUSBManager(aLogger));
            bpServer.AddDeviceSubtypeManager(aLogger => new HidManager(aLogger));

            return bpServer;
        }
        public ButtplugServer GetServer()
        {
            return InitializeButtplugServer("CLI Server", 0);
        }
    }
}