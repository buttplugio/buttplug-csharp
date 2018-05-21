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
        private class DeviceListItem
        {
            public ButtplugDeviceInfo Info;

            public bool Connected;

            public DeviceListItem(ButtplugDeviceInfo aInfo)
            {
                Info = aInfo;
                Connected = true;
            }

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

        public event EventHandler StartScanning;

        public event EventHandler StopScanning;

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

            // We can only attach our window closing handler over load is complete.
            Loaded += OnControlLoad;
        }

        public void SetButtplugServer(ButtplugServer aServer)
        {
            _bpServer = aServer;
            _bpServer.MessageReceived += OnMessageReceived;
        }

        public void Reset()
        {
            _devices.Clear();
            StoppedScanning();
        }

        public void DeviceAdded(ButtplugDeviceInfo aDev)
        {
            var devAdd = from dl in _devices
                         where dl.Info.Index == aDev.Index
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
                _devices.Add(new DeviceListItem(aDev));
            }
        }

        public void DeviceRemoved(uint aIndex)
        {
            var devRem = from dl in _devices
                         where dl.Info.Index == aIndex
                         select dl;
            foreach (var dr in devRem.ToList())
            {
                dr.Connected = false;
                _devices.UpdateList();
            }
        }

        private void OnControlLoad(object aObj, RoutedEventArgs aArgs)
        {
            // Attach a closing handler to the window, to make sure scanning stops when the
            // application is closed.
            var window = Window.GetWindow(this);
            window.Closing += OnWindowClosing;
        }

        private async void OnWindowClosing(object aSender, CancelEventArgs aE)
        {
            // If server is live, stop scanning, and clear our handler.
            if (_bpServer != null)
            {
                await _bpServer?.SendMessage(new StopScanning());
            }
            var window = Window.GetWindow(this);
            window.Closing -= OnWindowClosing;
        }

        private void OnMessageReceived(object aObj, MessageReceivedEventArgs aEvent)
        {
            var op = Dispatcher.InvokeAsync(() =>
            {
                switch (aEvent.Message)
                {
                    case DeviceAdded m:
                        DeviceAdded(new ButtplugDeviceInfo(m.DeviceIndex, m.DeviceName, m.DeviceMessages));
                        break;

                    case DeviceRemoved d:
                        DeviceRemoved(d.DeviceIndex);
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

        public void StoppedScanning()
        {
            ScanButton.Content = "Start Scanning";
        }

        private async void ScanButton_Click(object aSender, RoutedEventArgs aEvent)
        {
            // Disable button until we're done here
            ScanButton.Click -= ScanButton_Click;
            if ((string)ScanButton.Content == "Start Scanning")
            {
                StartScanning?.Invoke(this, new EventArgs());
                if (_bpServer != null)
                {
                    await _bpServer.SendMessage(new StartScanning());
                }

                ScanButton.Content = "Stop Scanning";
            }
            else
            {
                StopScanning?.Invoke(this, new EventArgs());
                if (_bpServer != null)
                {
                    await _bpServer?.SendMessage(new StopScanning());
                }

                ScanButton.Content = "Start Scanning";
            }

            ScanButton.Click += ScanButton_Click;
        }
    }
}
