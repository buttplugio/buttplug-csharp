using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Buttplug.Components.Controls
{
    /// <summary>
    /// Interaction logic for ButtplugDeviceControl.xaml
    /// </summary>
    public partial class ButtplugDeviceControl : UserControl
    {
        private class DeviceList : ObservableCollection<ButtplugDeviceInfo>
        {
        }

        public event EventHandler<List<ButtplugDeviceInfo>> DeviceSelectionChanged;

        private readonly DeviceList _devices;

        [NotNull]
        private readonly List<DispatcherOperation> _ops;

        private ButtplugServer _bpServer;

        public ButtplugDeviceControl()
        {
            InitializeComponent();
            _devices = new DeviceList();
            InitializeComponent();
            DeviceListBox.ItemsSource = _devices;
            DeviceListBox.SelectionMode = SelectionMode.Multiple;
            DeviceListBox.SelectionChanged += SelectionChangedHandler;
            _ops = new List<DispatcherOperation>();
        }

        public void SetButtplugServer(ButtplugServer aServer)
        {
            _bpServer = aServer;
            _bpServer.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object aObj, MessageReceivedEventArgs aEvent)
        {
            var op = Dispatcher.InvokeAsync(() =>
            {
                switch (aEvent.Message)
                {
                    case DeviceAdded m:
                        _devices.Add(new ButtplugDeviceInfo(m.DeviceIndex, m.DeviceName, m.DeviceMessages));
                        break;

                    case DeviceRemoved d:
                        var device = from dl in _devices
                                     where dl.Index == d.DeviceIndex
                                     select dl;
                        foreach (var dr in device.ToList())
                        {
                            _devices.Remove(dr);
                        }
                        break;
                }
            });
            _ops.Add(op);
            op.Completed += OperationCompletedHandler;
        }

        private void OperationCompletedHandler(object aObj, EventArgs aEvent)
        {
            _ops.Remove(aObj as DispatcherOperation);
        }

        private void SelectionChangedHandler(object aObj, EventArgs aEvent)
        {
            DeviceSelectionChanged?.Invoke(this, DeviceListBox.SelectedItems.Cast<ButtplugDeviceInfo>().ToList());
        }

        private async void ScanButton_Click(object aSender, RoutedEventArgs aEvent)
        {
            if (_bpServer == null)
            {
                return;
            }

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