using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Buttplug.Core;
using Buttplug.Messages;
using ButtplugKiirooPlatformEmulator;
using JetBrains.Annotations;
using System.Net;

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

    public partial class KiirooEmulatorPanel
    {
        private readonly ButtplugService _bpServer;
        private readonly DeviceList _devices;

        [NotNull]
        private readonly KiirooPlatformEmulator _kiirooEmulator;

        [NotNull]
        private readonly KiirooMessageTranslator _translator;

        [NotNull]
        private readonly List<DispatcherOperation> _ops;

        public KiirooEmulatorPanel(ButtplugService aBpService)
        {
            _bpServer = aBpService;
            _bpServer.SendMessage(new RequestServerInfo("Kiiroo Emulator")).Wait();
            _devices = new DeviceList();
            InitializeComponent();
            DeviceListBox.ItemsSource = _devices;
            _bpServer.MessageReceived += OnMessageReceived;
            _kiirooEmulator = new KiirooPlatformEmulator();
            _kiirooEmulator.OnKiirooPlatformEvent += HandleKiirooPlatformMessage;
            _kiirooEmulator.OnException += HandleKiirooPlatformMessage;
            _translator = new KiirooMessageTranslator();
            _translator.VibrateEvent += OnVibrateEvent;
            DeviceListBox.SelectionMode = SelectionMode.Multiple;
            DeviceListBox.SelectionChanged += SelectionChangedHandler;
            _ops = new List<DispatcherOperation>();
        }

        ~KiirooEmulatorPanel()
        {
            StopServer();
            _ops.ForEach(aDispatchOp =>
            {
                try
                {
                    aDispatchOp.Wait();
                }
                catch (TaskCanceledException)
                {
                }
            });
        }

        private void SelectionChangedHandler(object aObj, EventArgs aEvent)
        {
            var currentDevices = DeviceListBox.SelectedItems.Cast<Device>().ToList();
            if (currentDevices.Any(aDevice => aDevice.Messages.Contains("SingleMotorVibrateCmd")))
            {
                _translator.StartVibrateTimer();
                return;
            }

            _translator.StopVibrateTimer();
        }

        public void StartServer()
        {
            _kiirooEmulator.StartServer();
            ServerButton.Content = "Stop Server";
        }

        private void HandleKiirooPlatformMessage(object aObj, UnhandledExceptionEventArgs aEvent)
        {
            // This is most likely a socket in use exception
            switch (aEvent.ExceptionObject)
            {
                case HttpListenerException hle:
                    MessageBox.Show(hle.Message, "Error Encountered", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    StopServer();
                    if (hle.ErrorCode != 183)
                    {
                        throw hle;
                    }

                    break;

                default:
                    throw aEvent.ExceptionObject as Exception;
            }
        }

        public void StopServer()
        {
            _bpServer.SendMessage(new StopScanning()).Wait();
            _bpServer.SendMessage(new StopAllDevices()).Wait();
            _kiirooEmulator.StopServer();
            ServerButton.Content = "Start Server";
        }

        private void OperationCompletedHandler(object aObj, EventArgs aEvent)
        {
            _ops.Remove(aObj as DispatcherOperation);
        }

        private void OnVibrateEvent(object aObj, VibrateEventArgs aEvent)
        {
            var op = Dispatcher.InvokeAsync(async () =>
            {
                var currentDevices = DeviceListBox.SelectedItems.Cast<Device>().ToList();
                foreach (var device in currentDevices)
                {
                    if (device.Messages.Contains("SingleMotorVibrateCmd"))
                    {
                        await _bpServer.SendMessage(new SingleMotorVibrateCmd(device.Index, aEvent.VibrateValue));
                    }
                }
            });
            _ops.Add(op);
            op.Completed += OperationCompletedHandler;
        }

        private void OnMessageReceived(object aObj, MessageReceivedEventArgs aEvent)
        {
            var op = Dispatcher.InvokeAsync(() =>
            {
                switch (aEvent.Message)
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

        private async void ScanButton_Click(object aSender, RoutedEventArgs aEvent)
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

        private async void HandleKiirooPlatformMessage(object aObj, KiirooPlatformEventArgs aEvent)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                var currentDevices = DeviceListBox.SelectedItems.Cast<Device>().ToList();
                FleshlightLaunchFW12Cmd currentTranslatedCommand = null;
                foreach (var device in currentDevices)
                {
                    if (device.Messages.Contains(typeof(KiirooCmd).Name))
                    {
                        await _bpServer.SendMessage(new KiirooCmd(device.Index, aEvent.Position));
                    }
                    else if (device.Messages.Contains("FleshlightLaunchFW12Cmd") ||
                             device.Messages.Contains("SingleMotorVibrateCmd"))
                    {
                        if (currentTranslatedCommand == null)
                        {
                            currentTranslatedCommand = _translator.Translate(new KiirooCmd(device.Index, aEvent.Position));
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

        private void ServerButton_Click(object sender, RoutedEventArgs e)
        {
            if ((string)ServerButton.Content == "Start Server")
            {
                StartServer();
            }
            else
            {
                StopServer();
            }
        }
    }
}
