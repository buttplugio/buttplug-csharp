using Buttplug.Client;
using Buttplug.Core.Logging;
using Buttplug.Server.Managers.XamarinManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Server;
using Buttplug.Server.Connectors.WebsocketServer;
using Xamarin.Forms;
using DeviceAddedEventArgs = Buttplug.Client.DeviceAddedEventArgs;

namespace SampleXamarin
{
    public partial class MainPage : ContentPage
    {
        private ButtplugClient _client;
        private ButtplugClientDevice _device;
        private ButtplugWebsocketServer _wsserver;

        public MainPage()
        {
            InitializeComponent();
            var t = Task.Run(async () =>
            {
                /*
                var connector = new ButtplugEmbeddedConnector("Example Server");
                connector.Server.AddDeviceSubtypeManager<Buttplug.Server.DeviceSubtypeManager>(aLogger => new XamarinBluetoothManager(new ButtplugLogManager()));
                _client = new ButtplugClient("Example Client", connector);
                await _client.ConnectAsync();
                Device.BeginInvokeOnMainThread(() =>
                {
                    btnSync.IsEnabled = true;
                });
                _client.DeviceAdded += HandleDeviceAdded;
                _client.DeviceRemoved += HandleDeviceRemoved;
                */
                _wsserver = new ButtplugWebsocketServer();
                await _wsserver.StartServerAsync(() =>
                {
                    var server = new ButtplugServer("Android Server", 0);
                    server.AddDeviceSubtypeManager<Buttplug.Server.DeviceSubtypeManager>(aLogger =>
                        new XamarinBluetoothManager(new ButtplugLogManager()));
                    return server;
                }, aLocalOnly: false);
            });
        }

        async void HandleDeviceAdded(object aObj, DeviceAddedEventArgs aArgs)
        {
            _device = aArgs.Device;
            await _client.StopScanningAsync();
            Device.BeginInvokeOnMainThread(() =>
            {
                btnVibrate.IsEnabled = true;
            });

        }

        void HandleDeviceRemoved(object aObj, DeviceRemovedEventArgs aArgs)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                btnVibrate.IsEnabled = false;
            });

            Console.WriteLine($"Device connected: {aArgs.Device.Name}");
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            var t = Task.Run(() => ScanForDevices());
        }

        async Task ScanForDevices()
        {
            Console.WriteLine("Scanning for devices until key is pressed. Found devices will be printed to console.");
            await _client.StartScanningAsync();
        }

        private async void Vibrate_Clicked(object sender, EventArgs e)
        {
            await _device.SendVibrateCmd(0.5);
            btnStop.IsEnabled = true;
            btnVibrate.IsEnabled = false;
        }

        private async void Stop_Clicked(object sender, EventArgs e)
        {
            await _device.SendVibrateCmd(0.0);
            btnStop.IsEnabled = false;
            btnVibrate.IsEnabled = true;
        }
    }
}
