﻿using System;
using Buttplug.Core;
using Buttplug.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ButtplugUWPBluetoothManager.Core;
using ButtplugXInputGamepadManager.Core;
using NLog;
using NLog.Config;
using NLog.Targets;
using JetBrains.Annotations;
using Microsoft.Win32;
using SharpRaven;
using SharpRaven.Data;

namespace ButtplugControlLibrary
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ButtplugTabControl : UserControl
    {
        public ButtplugService BpServer { get; }
        private readonly RavenClient _ravenClient;
        private bool _sentCrashLog;
        private Logger _guiLog;

        public ButtplugTabControl()
        {
            _ravenClient = new RavenClient("https://2e376d00cdcb44bfb2140c1cf000d73b:1fa6980aeefa4b048b866a450ee9ad71@sentry.io/170313");


            var c = LogManager.Configuration ?? new LoggingConfiguration();

            // Cover all of the possible bases for WPF failure
            // http://stackoverflow.com/questions/12024470/unhandled-exception-still-crashes-application-after-being-caught

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            // Null check application, otherwise test bringup for GUI tests will fail
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            }

            _guiLog = LogManager.GetCurrentClassLogger();
            LogManager.Configuration = LogManager.Configuration ?? new LoggingConfiguration();
#if DEBUG
// Debug Logger Setup
            var t = new DebuggerTarget();
            LogManager.Configuration.AddTarget("debugger", t);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, t));
            LogManager.Configuration = LogManager.Configuration;
#endif
            // Set up internal services
            BpServer = new ButtplugService();

            // Set up GUI
            InitializeComponent();

            try
            {
                AboutControl.InitializeVersion();;
                var version = AboutControl.GetAboutVersion();
                _guiLog.Info($"Buttplug Server Revision: {version}");
            }
            catch (Exception)
            {
                // TODO Make this catch far more granular
                _guiLog.Info("Can't load assembly file, no version info available!");
            }

            AboutControl.AboutImageClickedABunch += (o, e) => DeveloperTab.Visibility = Visibility.Visible;
        }

        public void InitializeButtplugServer()
        {
            if (!(Environment.OSVersion is null))
            {
                _guiLog.Info($"Windows Version: {Environment.OSVersion.VersionString}");
            }
            else
            {
                _guiLog.Error("Cannot retreive Environment.OSVersion string.");
            }

            var releaseId = 0;
            try
            {
                releaseId = Int32.Parse(Registry
                    .GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "")
                    .ToString());
                _guiLog.Info($"Windows Release ID: {releaseId}");
            }
            catch (Exception)
            {
                _guiLog.Error("Cannot retreive Release ID for OS! Will not load bluetooth manager.");
            }

            
            // Make sure we're on the Creators update before even trying to load the UWP Bluetooth Manager
            if (releaseId >= 1703)
            {
                if (!UWPBluetoothManager.HasRegistryKeysSet())
                {
                    _guiLog.Error("Registry keys not set for UWP bluetooth API security. This may cause Bluetooth devices to not be seen.");
                    // TODO Need to figure out how to only show this if we're running a full application.
                    MessageBox.Show("Registry keys not set for UWP bluetooth API security. This may cause Bluetooth devices to not be seen.");
                }
                try
                {
                    BpServer.AddDeviceSubtypeManager(aLogger => new UWPBluetoothManager(aLogger));
                }
                catch (PlatformNotSupportedException e)
                {
                    SendExceptionToSentry(e);
                }
            }
            else
            {
                _guiLog.Warn("OS Version too old to load bluetooth core. Must be Windows 10 15063 or higher.");
            }

            BpServer.AddDeviceSubtypeManager(aLogger => new XInputGamepadManager(aLogger));
        }

        private void SendExceptionToSentry(Exception aEx)
        {
            if (_sentCrashLog)
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

            MessageBoxResult result = MessageBox.Show("An error was encountered! Do you want to report this to the developers?", "Error encountered", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
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

        private void CrashButton_Click(object sender, RoutedEventArgs e)
        {
            throw new Exception("Should be caught and sent to sentry!");
        }

        public void SetApplicationTab(string aTabName, UserControl aTabControl)
        {
            ApplicationTab.Header = aTabName;
            ApplicationTab.Content = aTabControl;            
        }
    }

}

