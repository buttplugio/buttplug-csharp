using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Buttplug.Core;
using Buttplug.Messages;
using ButtplugKiirooPlatformEmulator;

namespace ButtplugKiirooEmulatorGUI
{
    public class Device
    {
        private string Name { get; }
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

    public partial class KiirooEmulatorPanel : UserControl
    {
        private readonly ButtplugService _bpServer;
        private readonly DeviceList _devices;
        private readonly KiirooPlatformEmulator _kiirooEmulator;
        private KiirooMessageTranslator _translator;
        private List<DispatcherOperation> _ops;

        public KiirooEmulatorPanel(ButtplugService aBpService)
        {
            _bpServer = aBpService;
            _devices = new DeviceList();
            InitializeComponent();
            DeviceListBox.ItemsSource = _devices;
            _bpServer.MessageReceived += OnMessageReceived;
            _kiirooEmulator = new KiirooPlatformEmulator();
            _kiirooEmulator.OnKiirooPlatformEvent += HandleKiirooPlatformMessage;
            _translator = new KiirooMessageTranslator();
            _translator.VibrateEvent += OnVibrateEvent;
            DeviceListBox.SelectionMode = SelectionMode.Multiple;
            DeviceListBox.SelectionChanged += SelectionChangedHandler;
            _ops = new List<DispatcherOperation>();            
        }

        ~KiirooEmulatorPanel()
        {
            StopServer();
            _ops.ForEach((x) =>
            {
                try
                {
                    x.Wait();
                }
                catch (TaskCanceledException e)
                {
                }
            });
        }

        public void SelectionChangedHandler(object o, EventArgs e)
        {
            var currentDevices = DeviceListBox.SelectedItems.Cast<Device>().ToList();
            foreach (var device in currentDevices)
            {
                if (device.Messages.Contains("SingleMotorVibrateCmd"))
                {
                    _translator.StartVibrateTimer();
                    return;
                }
            }
            _translator.StopVibrateTimer();
        }

        public void StartServer()
        {
            _kiirooEmulator.StartServer();
        }

        public void StopServer()
        {
            _kiirooEmulator.StopServer();
        }

        void OperationCompletedHandler(object o, EventArgs e)
        {
            _ops.Remove(o as DispatcherOperation);
        }

        private void OnVibrateEvent(object o, VibrateEventArgs e)
        {
            var op = Dispatcher.InvokeAsync(async () =>
            {
                var currentDevices = DeviceListBox.SelectedItems.Cast<Device>().ToList();
                foreach (var device in currentDevices)
                {
                    if (device.Messages.Contains("SingleMotorVibrateCmd"))
                    {
                        await _bpServer.SendMessage(new SingleMotorVibrateCmd(device.Index, e.VibrateValue));
                    }
                }
            });
            _ops.Add(op);
            op.Completed += OperationCompletedHandler;

        }

        private  void OnMessageReceived(object o, MessageReceivedEventArgs e)
        {
            var op = Dispatcher.InvokeAsync(() =>
            {
                switch (e.Message)
                {
                    case DeviceAdded m:
                        _devices.Add(new Device(m.DeviceIndex, m.DeviceName, m.DeviceMessages));
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
                var currentDevices = DeviceListBox.SelectedItems.Cast<Device>().ToList();
                FleshlightLaunchFW12Cmd currentTranslatedCommand = null;
                foreach (var device in currentDevices)
                {
                    if (device.Messages.Contains("KiirooRawCmd"))
                    {
                        await _bpServer.SendMessage(new KiirooCmd(device.Index, e.Position));
                    }
                    else if (device.Messages.Contains("FleshlightLaunchFW12Cmd") ||
                             device.Messages.Contains("SingleMotorVibrateCmd"))
                    {
                        if (currentTranslatedCommand == null)
                        {
                            currentTranslatedCommand = _translator.Translate(new KiirooCmd(device.Index, e.Position));
                        }
                        currentTranslatedCommand.DeviceIndex = device.Index;
                        if (device.Messages.Contains("FleshlightLaunchFW12Cmd"))
                        {
                            await _bpServer.SendMessage(currentTranslatedCommand);
                        }
                    }
                }
            });
        }
    }
}
