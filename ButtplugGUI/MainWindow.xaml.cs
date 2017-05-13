using System;
using System.Collections.Generic;
using Buttplug.Core;
using Buttplug.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ButtplugKiirooPlatformEmulator;

namespace ButtplugGUI
{
    public class Device
    {
        public string Name { get; }
        public uint Index { get; }
        public string[] Messages { get;  }

        public Device(uint aIndex, string aName, string[] aMessages)
        {
            Index = aIndex;
            Name = aName;
            Messages = aMessages;
        }

        public override string ToString()
        {
            return $"{Index}: {Name}";
        }
    }

    public class DeviceList : ObservableCollection<Device>
    {
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ButtplugService _bpServer;
        private readonly DeviceList _devices;
        private KiirooPlatformEmulator _kiirooEmulator;

        public MainWindow()
        {
            // Set up internal services
            _bpServer = new ButtplugService();
            _bpServer.MessageReceived += OnMessageReceived;
            _devices = new DeviceList();
            _kiirooEmulator = new KiirooPlatformEmulator();
            _kiirooEmulator.OnKiirooPlatformEvent += HandleKiirooPlatformMessage;
            _kiirooEmulator.StartServer();
            
            // Set up GUI
            InitializeComponent();
            DeviceListBox.ItemsSource = _devices;
            KiirooListBox.ItemsSource = _devices;
        }

        public void OnMessageReceived(object o, MessageReceivedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                switch (e.Message)
                {
                    case DeviceAdded m:
                        _devices.Add(new Device(m.DeviceIndex, m.DeviceName, m.DeviceMessages));
                        break;
                }
            });
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable button until we're done here
            ScanButton.Click -= ScanButton_Click;
            if ((string)ScanButton.Content == "Start Scanning")
            {
                await _bpServer.SendMessage(new StartScanning());
                ScanButton.Content = "Stop Scanning";
            }
            else
            {
                await _bpServer.SendMessage(new StopScanning());
                ScanButton.Content = "Start Scanning";
            }
            ScanButton.Click += ScanButton_Click;
        }

        public async void HandleKiirooPlatformMessage(object o, KiirooPlatformEventArgs e)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                var currentDevices = KiirooListBox.SelectedItems.Cast<Device>().ToList();
                foreach (var device in currentDevices)
                {
                    if (!device.Messages.Contains("KiirooRawCmd"))
                    {
                        continue;
                    }
                    await _bpServer.SendMessage(new KiirooRawCmd(device.Index, e.Position));
                }
            });
        }

        private void ApplicationSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            WebsocketSettingsGrid.Visibility = Visibility.Hidden;
            KiirooSettingsGrid.Visibility = Visibility.Hidden;
            if (ApplicationSelector.SelectedItem == ApplicationNone)
            {
            }
            else if (ApplicationSelector.SelectedItem == ApplicationWebsockets)
            {
                WebsocketSettingsGrid.Visibility = Visibility.Visible;
            }
            else if (ApplicationSelector.SelectedItem == ApplicationKiiroo)
            {
                KiirooSettingsGrid.Visibility = Visibility.Visible;
            }
        }
    }
}