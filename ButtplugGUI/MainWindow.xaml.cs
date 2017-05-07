using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Buttplug;
using System.Collections.ObjectModel;
using Buttplug.Core;
using Buttplug.Messages;

namespace ButtplugGUI
{

    public class Device
    {
        public string Name { get; }
        public uint Index { get; }

        public Device(uint aIndex, string aName)
        {
            Index = aIndex;
            Name = aName;
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
        public MainWindow()
        {
            InitializeComponent();
            _bpServer = new ButtplugService();
            _bpServer.MessageReceived += OnMessageReceived;
            _devices = new DeviceList();
            DeviceListBox.ItemsSource = _devices;
        }

        public void OnMessageReceived(object o, MessageReceivedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                switch (e.Message)
                {
                    case Buttplug.Messages.DeviceAdded m:
                        _devices.Add(new Device(m.DeviceIndex, m.DeviceName));
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
    }
}
