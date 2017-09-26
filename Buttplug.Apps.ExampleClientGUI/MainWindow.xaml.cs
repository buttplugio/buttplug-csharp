using System.Collections.Generic;
using System.Windows;
using Buttplug.Client;
using Buttplug.Components.Controls;

namespace Buttplug.Apps.ExampleClientGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private ExampleClientPanel _clientPanel;

        public MainWindow()
        {
            InitializeComponent();
            if (Application.Current == null)
            {
                return;
            }

            _clientPanel = new ExampleClientPanel(ButtplugTab.AddDevicePanel());
            ButtplugTab.SetServerDetails("Buttplug Client", 0);
            ButtplugTab.SetApplicationTab("Buttplug Client", _clientPanel);

            ButtplugTab.SelectedDevicesChanged += SelectionChangedHandler;
        }

        private void SelectionChangedHandler(object aObj, List<ButtplugDeviceInfo> aDevices)
        {
            foreach (var dev in aDevices)
            {
                var cdev = new ButtplugClientDevice(dev.Index, dev.Name, dev.Messages);
                _clientPanel.Devices.AddOrUpdate(dev.Index, cdev, (idx, old) => cdev);
            }

            List<uint> ids = new List<uint>();
            foreach (var dev in aDevices)
            {
                ids.Add(dev.Index);
            }

            foreach (var dev in _clientPanel.Devices.Values)
            {
                if (!ids.Contains(dev.Index))
                {
                    _clientPanel.Devices.TryRemove(dev.Index, out var old);
                }
            }
        }
    }
}