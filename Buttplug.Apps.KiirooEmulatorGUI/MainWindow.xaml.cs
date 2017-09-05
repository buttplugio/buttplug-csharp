using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using Buttplug.Components.Controls;
using Buttplug.Components.KiirooPlatformEmulator;
using Buttplug.Core.Messages;
using Buttplug.Server;
using JetBrains.Annotations;
using System.Threading;

namespace Buttplug.Apps.KiirooEmulatorGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        [NotNull]
        private readonly KiirooPlatformEmulator _kiirooEmulator;

        [NotNull]
        private readonly KiirooMessageTranslator _translator;

        [NotNull]
        private readonly List<DispatcherOperation> _ops;

        private readonly ButtplugServer _bpServer;
        private List<ButtplugDeviceInfo> _devices = new List<ButtplugDeviceInfo>();
        private KiirooEmulatorPanel _emu;

        public MainWindow()
        {
            InitializeComponent();
            if (Application.Current == null)
            {
                return;
            }

            ButtplugTab.SetServerDetails("Kiiroo Emulator", 0);
            _bpServer = ButtplugTab.GetServer();
            _bpServer.SendMessage(new RequestServerInfo("Kiiroo Emulator")).Wait();
            InitializeComponent();
            _kiirooEmulator = new KiirooPlatformEmulator();
            _kiirooEmulator.OnKiirooPlatformEvent += HandleKiirooPlatformMessage;
            _kiirooEmulator.OnException += HandleKiirooPlatformMessage;
            _translator = new KiirooMessageTranslator();
            _translator.VibrateEvent += OnVibrateEvent;
            _ops = new List<DispatcherOperation>();
            var emu = new KiirooEmulatorPanel();
            ButtplugTab.AddDevicePanel(_bpServer);
            ButtplugTab.SetApplicationTab("Kiiroo Emulator", emu);
            Closing += ClosingHandler;
            StartServer();
            emu.ServerStatusChanged += OnServerStatusChanged;
            ButtplugTab.SelectedDevicesChanged += SelectionChangedHandler;
        }

        private void OnServerStatusChanged(object aObj, bool aIsRunning)
        {
            if (aIsRunning)
            {
                StartServer();
                return;
            }

            StopServer();
        }

        private void ClosingHandler(object aObj, CancelEventArgs e)
        {
            StopServer();
        }

        private void SelectionChangedHandler(object aObj, List<ButtplugDeviceInfo> aDevices)
        {
            _devices = aDevices;
            if (_devices.Any(aDevice => aDevice.Messages.Contains("SingleMotorVibrateCmd")))
            {
                _translator.StartVibrateTimer();
                return;
            }

            _translator.StopVibrateTimer();
        }

        public void StartServer()
        {
            _kiirooEmulator.StartServer();
        }

        private void HandleKiirooPlatformMessage(object aObj, UnhandledExceptionEventArgs aEvent)
        {
            // This is most likely a socket in use exception
            switch (aEvent.ExceptionObject)
            {
                case HttpListenerException hle:
                    new Thread(() => MessageBox.Show(hle.Message, "Error Encountered", MessageBoxButton.OK, MessageBoxImage.Exclamation)).Start();
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

        public async void StopServer()
        {
            _kiirooEmulator.StopServer();
            await _bpServer.SendMessage(new StopScanning());
            await _bpServer.SendMessage(new StopAllDevices());
        }

        private void OperationCompletedHandler(object aObj, EventArgs aEvent)
        {
            _ops.Remove(aObj as DispatcherOperation);
        }

        private void OnVibrateEvent(object aObj, VibrateEventArgs aEvent)
        {
            var op = Dispatcher.InvokeAsync(async () =>
            {
                foreach (var device in _devices)
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

        private async void HandleKiirooPlatformMessage(object aObj, KiirooPlatformEventArgs aEvent)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                FleshlightLaunchFW12Cmd currentTranslatedCommand = null;
                foreach (var device in _devices)
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
    }
}