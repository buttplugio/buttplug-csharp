using Buttplug.Apps.GameVibrationRouter.Interface;
using Buttplug.Apps.GameVibrationRouter.Payload;
using Buttplug.Components.Controls;
using Buttplug.Core.Messages;
using Buttplug.Server;
using EasyHook;
using JetBrains.Annotations;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace Buttplug.Apps.GameVibrationRouter.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        [NotNull]
        private readonly ButtplugServer _bpServer;

        [NotNull]
        private readonly Logger _log;

        [NotNull]
        private readonly ProcessTab _processTab;

        [NotNull]
        private readonly VibeGraphTab _graphTab;

        private IpcServerChannel _xinputHookServer;
        private string _channelName;
        private List<ButtplugDeviceInfo> _devices = new List<ButtplugDeviceInfo>();
        private Vibration _lastVibration = new Vibration();
        private Vibration _lastSentVibration = new Vibration();
        private Timer runTimer;
        private Timer commandTimer;
        private double _vibrationMultiplier = 1;

        public MainWindow()
        {
            InitializeComponent();
            if (Application.Current == null)
            {
                return;
            }
            ButtplugTab.GetLogControl().MaxLogs = 10000;

            ButtplugTab.SetServerDetails("Game Vibration Router Server", 0);
            _bpServer = ButtplugTab.GetServer();
            _log = LogManager.GetCurrentClassLogger();
            ButtplugGameVibrationRouterInterface.VibrationCommandReceived += OnVibrationCommand;
            ButtplugGameVibrationRouterInterface.VibrationPingMessageReceived += OnVibrationPingMessage;
            ButtplugGameVibrationRouterInterface.VibrationExceptionReceived += OnVibrationException;
            ButtplugGameVibrationRouterInterface.VibrationExitReceived += OnVibrationExit;
            Task.FromResult(_bpServer.SendMessage(new RequestServerInfo("Buttplug Game Vibration Router")));
            _graphTab = new VibeGraphTab();
            ButtplugTab.SetOtherTab("Vibes", _graphTab);
            _processTab = new ProcessTab();
            _processTab.ProcessAttachRequested += OnAttachRequested;
            _processTab.ProcessDetachRequested += OnDetachRequested;
            ButtplugTab.SetApplicationTab("Processes", _processTab);
            ButtplugTab.AddDevicePanel(_bpServer);
            ButtplugTab.SelectedDevicesChanged += OnSelectedDevicesChanged;

            _graphTab.MultiplierChanged += MultiplierChanged;
            _graphTab.PassthruChanged += PassthruChanged;

            var config = new ButtplugConfig("Buttplug");
            ButtplugTab.GetAboutControl().CheckUpdate(config, "buttplug-csharp");

            runTimer = new Timer {Interval = 100, AutoReset = true};
            runTimer.Elapsed += AddPoint;

            commandTimer = new Timer { Interval = 50, AutoReset = true };
            commandTimer.Elapsed += OnVibrationTimer;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;
        }

        private void MultiplierChanged(object sender, double vibeMultiplier)
        {
            _vibrationMultiplier = vibeMultiplier;
        }

        private void PassthruChanged(object sender, bool shouldPassthru)
        {
            ButtplugGameVibrationRouterInterface._shouldPassthru = shouldPassthru;
        }

        public void AddPoint(object o, ElapsedEventArgs e)
        {
            _graphTab.AddVibrationValue(_lastVibration.LeftMotorSpeed, _lastVibration.RightMotorSpeed);
        }

        private void OnSelectedDevicesChanged(object aObj, List<ButtplugDeviceInfo> aDevices)
        {
            _devices = aDevices;
        }

        private void OnVibrationException(object aObj, Exception aEx)
        {
            _log.Error($"Remote Exception: {aEx}");
            Dispatcher.Invoke(() =>
            {
                OnDetachRequested(this, true);
                _processTab.ProcessError = "Error attaching, see logs for details.";
            });
        }

        private void OnVibrationPingMessage(object aObj, string aMsg)
        {
            _log.Info($"Remote Ping Message: {aMsg}");
        }

        private void OnVibrationExit(object aObj, bool aTrue)
        {
            Dispatcher.Invoke(() =>
            {
                Detach();
                _processTab.ProcessError = "Attached process detached or exited";
            });
        }

        private void Detach()
        {
            ButtplugGameVibrationRouterInterface.Detach();
            _processTab.Attached = false;
            _channelName = null;
            _xinputHookServer = null;
            runTimer.Enabled = false;
            commandTimer.Enabled = false;
        }

        private async void OnVibrationTimer(object aObj, ElapsedEventArgs e)
        {
            if (_lastVibration == _lastSentVibration)
            {
                return;
            }
            _lastSentVibration = _lastVibration;
            await Dispatcher.Invoke(async () =>
            {
                foreach (var device in _devices)
                {
                    // For now, we only handle devices that can take vibration messages.
                    if (!device.SupportsMessage(typeof(SingleMotorVibrateCmd)))
                    {
                        continue;
                    }
                    double vibeSpeed = (_lastVibration.LeftMotorSpeed + _lastVibration.RightMotorSpeed) / (2.0 * 65535.0);
                    await _bpServer.SendMessage(new SingleMotorVibrateCmd(device.Index,
                        Math.Min(vibeSpeed * _vibrationMultiplier, 1.0)));
                }
            });
        }

        private void OnVibrationCommand(object aObj, Vibration aVibration)
        {
            aVibration.LeftMotorSpeed = Convert.ToUInt16(aVibration.LeftMotorSpeed * _vibrationMultiplier > 65535 ? 65535 : aVibration.LeftMotorSpeed * _vibrationMultiplier);
            aVibration.RightMotorSpeed = Convert.ToUInt16(aVibration.RightMotorSpeed * _vibrationMultiplier > 65535 ? 65535 : aVibration.RightMotorSpeed * _vibrationMultiplier);
            _lastVibration = aVibration;
        }

        private void OnAttachRequested(object aObject, int aProcessId)
        {
            try
            {
                _xinputHookServer = RemoteHooking.IpcCreateServer<ButtplugGameVibrationRouterInterface>(
                    ref _channelName,
                    WellKnownObjectMode.Singleton);
                var dllFile = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(typeof(ButtplugGameVibrationRouterPayload).Assembly.Location), 
                    "Buttplug.Apps.GameVibrationRouter.Payload.dll");
                _log.Info($"Beginning process injection on {aProcessId}...");
                _log.Info($"Injecting DLL {dllFile}");
                RemoteHooking.Inject(
                    aProcessId,
                    InjectionOptions.Default,
                    dllFile,
                    dllFile,
                    // the optional parameter list...
                    _channelName);
                _log.Info($"Finished process injection on {aProcessId}...");
                _processTab.Attached = true;
                _processTab.ProcessError = "Attached to process";
                runTimer.Enabled = true;
                commandTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                Detach();
                _log.Error(ex);
                _processTab.ProcessError = "Error attaching, see logs for details.";
            }
        }

        private void OnDetachRequested(object aObject, bool aShouldDetach)
        {
            Detach();
        }
    }
}
