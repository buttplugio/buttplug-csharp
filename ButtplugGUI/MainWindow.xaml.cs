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
using Buttplug.Messages;

namespace ButtplugGUI
{

    public class Device
    {
        public String Name { get; }
        public uint Index { get; }

        public Device(uint aIndex, String aName)
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
        ButtplugService BPServer;
        DeviceList Devices;
        public MainWindow()
        {
            InitializeComponent();
            BPServer = new ButtplugService();
            BPServer.MessageReceived += OnMessageReceived;
            Devices = new DeviceList();
            DeviceListBox.ItemsSource = Devices;
        }

        public void OnMessageReceived(object o, MessageReceivedEventArgs e)
        {
            switch (e.Message)
            {
                case DeviceAddedMessage m:
                    Devices.Add(new Device(m.DeviceIndex, m.DeviceName));
                    break;
            }
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if ((string)ScanButton.Content == "Start Scanning")
            {
                BPServer.StartScanning();
                ScanButton.Content = "Stop Scanning";
            }
            else
            {
                BPServer.StopScanning();
                ScanButton.Content = "Start Scanning";
            }
           
        }
    }
}
