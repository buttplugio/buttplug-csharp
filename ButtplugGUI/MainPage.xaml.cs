using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
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

using Buttplug;

namespace ButtplugGUI
{
    /// <summary>
    /// Application for connecting to sex toys and controlling them in generic ways.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private ButtplugService mButtplug;
        public MainPage()
        {
            this.InitializeComponent();
            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = false;
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            ApplicationView.PreferredLaunchViewSize = new Size(600, 400);
            mButtplug = new ButtplugService();
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
