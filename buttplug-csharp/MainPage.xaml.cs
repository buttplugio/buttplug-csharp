using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.ViewManagement;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Threading;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace buttplug_csharp
{
    /// <summary>
    /// Application for connecting to sex toys and controlling them in generic ways.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        static readonly Guid RAUNCH_SERVICE = new Guid("88f80580-0000-01e6-aace-0002a5d5c51b");
        static readonly Guid RAUNCH_TX_CHAR = new Guid("88f80581-0000-01e6-aace-0002a5d5c51b");
        static readonly Guid RAUNCH_RX_CHAR = new Guid("88f80582-0000-01e6-aace-0002a5d5c51b");
        static readonly Guid RAUNCH_CMD_CHAR = new Guid("88f80583-0000-01e6-aace-0002a5d5c51b");

        const int BLEWATCHER_STOP_TIMEOUT = 1;          // minute
        private DeviceWatcher deviceWatcher = null;

        //Handlers for device detection
        private TypedEventHandler<DeviceWatcher, DeviceInformation> handlerAdded = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerUpdated = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerRemoved = null;
        private TypedEventHandler<DeviceWatcher, Object> handlerEnumCompleted = null;

        private DeviceWatcher blewatcher = null;
        private EventWaitHandle blewatcherStopped = new EventWaitHandle(false, EventResetMode.AutoReset);

        private Dictionary<int, GattDeviceService> _services = new Dictionary<int, GattDeviceService>();
        private Dictionary<int, GattCharacteristic> _notifyList = new Dictionary<int, GattCharacteristic>();

        private BluetoothLEAdvertisementWatcher bleWatcher = null;
        

        public MainPage()
        {
            this.InitializeComponent();
            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = false;
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            ApplicationView.PreferredLaunchViewSize = new Size(600, 400);
            bleWatcher = new BluetoothLEAdvertisementWatcher();
            bleWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(RAUNCH_SERVICE);
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
