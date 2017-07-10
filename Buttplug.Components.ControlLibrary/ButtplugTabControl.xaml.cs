﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Buttplug.Server;
using ButtplugUWPBluetoothManager.Core;
using ButtplugXInputGamepadManager.Core;
using JetBrains.Annotations;
using Microsoft.Win32;
using NLog;
using NLog.Config;
#if DEBUG
using NLog.Targets;
#endif
using SharpRaven;
using SharpRaven.Data;

namespace Buttplug.Components.Controls
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ButtplugTabControl : IButtplugServiceFactory
    {
        private readonly RavenClient _ravenClient;
        [NotNull]
        private readonly Logger _guiLog;
        private int _releaseId;
        private string _serverName;
        private uint _maxPingTime;
        private bool _sentCrashLog;

        public ButtplugTabControl()
        {
            _guiLog = LogManager.GetCurrentClassLogger();
            LogManager.Configuration = LogManager.Configuration ?? new LoggingConfiguration();
#if DEBUG
            // Debug Logger Setup
            var t = new DebuggerTarget();
            LogManager.Configuration.AddTarget("debugger", t);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, t));
            LogManager.Configuration = LogManager.Configuration;
#endif

            try
            {
                _ravenClient = new RavenClient("SENTRY_API_URL");
            }
            catch (ArgumentException)
            {
                _guiLog.Error("Sentry URL invalid, cannot submit crash reports!");
            }
            // Cover all of the possible bases for WPF failure
            // http://stackoverflow.com/questions/12024470/unhandled-exception-still-crashes-application-after-being-caught
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            // Null check application, otherwise test bringup for GUI tests will fail
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            }

            // Set up GUI
            InitializeComponent();

            try
            {
                AboutControl.InitializeVersion();
                var version = AboutControl.GetAboutVersion();
                _guiLog.Info($"Buttplug Server Revision: {version}");
            }
            catch (Exception)
            {
                // TODO Make this catch far more granular
                _guiLog.Info("Can't load assembly file, no version info available!");
            }

            AboutControl.AboutImageClickedABunch += (aObj, aEvent) => DeveloperTab.Visibility = Visibility.Visible;
        }

        private ButtplugService InitializeButtplugServer(string aServerName, uint aMaxPingTime)
        {
            // Set up internal services
            var bpServer = new ButtplugService(aServerName, aMaxPingTime);
            if (!(Environment.OSVersion is null))
            {
                _guiLog.Info($"Windows Version: {Environment.OSVersion.VersionString}");
            }
            else
            {
                _guiLog.Error("Cannot retreive Environment.OSVersion string.");
            }

            // Make sure we're on the Creators update before even trying to load the UWP Bluetooth Manager
            if (_releaseId >= 1703)
            {
                try
                {
                    bpServer.AddDeviceSubtypeManager(aLogger => new UWPBluetoothManager(aLogger));
                }
                catch (PlatformNotSupportedException e)
                {
                    _guiLog.Warn(e, "Something went wrong whilst setting up bluetooth.");
                }
            }
            else
            {
                _guiLog.Warn("OS Version too old to load bluetooth core. Must be Windows 10 15063 or higher.");
            }

            bpServer.AddDeviceSubtypeManager(aLogger => new XInputGamepadManager(aLogger));
            return bpServer;
        }

        private void SendExceptionToSentry(Exception aEx)
        {
            if (_sentCrashLog || _ravenClient == null)
            {
                return;
            }

            _sentCrashLog = true;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException;
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException -= CurrentOnDispatcherUnhandledException;
            }

            if (Dispatcher != null)
            {
                Dispatcher.UnhandledException -= DispatcherOnUnhandledException;
            }

            var result = MessageBox.Show("An error was encountered! Do you want to report this to the developers?", "Error encountered", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                if (LogControl != null)
                {
                    aEx.Data.Add("LogMessages", string.Join("\n", LogControl.GetLogs()));
                }

                _ravenClient.Capture(new SentryEvent(aEx));
            }
        }

        private void DispatcherOnUnhandledException(object aObj, DispatcherUnhandledExceptionEventArgs aEx)
        {
            SendExceptionToSentry(aEx.Exception);
        }

        private void CurrentDomainOnUnhandledException(object aObj, UnhandledExceptionEventArgs aEx)
        {
            SendExceptionToSentry(aEx.ExceptionObject as Exception);
        }

        private void CurrentOnDispatcherUnhandledException(object aObj, DispatcherUnhandledExceptionEventArgs aEx)
        {
            SendExceptionToSentry(aEx.Exception);
        }

        private void CrashButton_Click(object aSender, RoutedEventArgs aEvent)
        {
            throw new Exception("Should be caught and sent to sentry!");
        }

        private void SendLogsButton_Click(object aSender, RoutedEventArgs aEvent)
        {
            _ravenClient?.Capture(new SentryEvent(string.Join("\n", LogControl.GetLogs())));
        }

        public void SetApplicationTab(string aTabName, UserControl aTabControl)
        {
            ApplicationTab.Header = aTabName;
            ApplicationTab.Content = aTabControl;
        }

        public void SetServerDetails(string serverName, uint maxPingTime)
        {
            _serverName = serverName;
            _maxPingTime = maxPingTime;

            try
            {
                _releaseId = int.Parse(Registry
                    .GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", string.Empty)
                    .ToString());
                _guiLog.Info($"Windows Release ID: {_releaseId}");
            }
            catch (Exception)
            {
                _guiLog.Error("Cannot retreive Release ID for OS! Will not load bluetooth manager.");
            }

            if (!UWPBluetoothManager.HasRegistryKeysSet())
            {
                _guiLog.Error("Registry keys not set for UWP bluetooth API security. This may cause Bluetooth devices to not be seen.");

                // Only show this if we're running a full application.
                if (Application.Current != null)
                {
                    MessageBox.Show("Registry keys not set for UWP bluetooth API security. This may cause Bluetooth devices to not be seen.");
                }
            }
        }

        public ButtplugService GetService()
        {
            if (_serverName == null)
            {
                throw new AccessViolationException("SetServerDetails() must be called before GetService()");
            }

            return InitializeButtplugServer(_serverName, _maxPingTime);
        }
    }
}