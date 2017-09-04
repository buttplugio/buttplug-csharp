using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server;
using JetBrains.Annotations;

namespace Buttplug.Components.Controls
{
    /// <summary>
    /// Interaction logic for ButtplugDeviceControl.xaml
    /// </summary>
    public partial class ButtplugDeviceControl : UserControl
    {
        public class DeviceListItem
        {
            public ButtplugDeviceInfo Info;

            public bool Connected;

            public DeviceListItem(ButtplugDeviceInfo aInfo)
            {
                Info = aInfo;
                Connected = true;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public override string ToString()
            {
                return $"{Info}" + (Connected ? string.Empty : " (disconnected)");
            }
        }

        private class DeviceList : ObservableCollection<DeviceListItem>
        {
            public void UpdateList()
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
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
                        var devAdd = from dl in _devices
                                     where dl.Info.Index == m.DeviceIndex
                                     select dl;
                        if (devAdd.Any())
                        {
                            foreach (var dr in devAdd.ToList())
                            {
                                dr.Connected = true;
                                _devices.UpdateList();
                            }
                        }
                        else
                        {
                            _devices.Add(new DeviceListItem(new ButtplugDeviceInfo(m.DeviceIndex, m.DeviceName, m.DeviceMessages)));
                        }
                        break;

                    case DeviceRemoved d:
                        var devRem = from dl in _devices
                                     where dl.Info.Index == d.DeviceIndex
                                     select dl;
                        foreach (var dr in devRem.ToList())
                        {
                            dr.Connected = false;
                            _devices.UpdateList();
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
            DeviceSelectionChanged?.Invoke(this,
                DeviceListBox.SelectedItems.Cast<DeviceListItem>()
                    .Where(aLI => aLI.Connected)
                    .Select(aLI => aLI.Info).ToList());
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
