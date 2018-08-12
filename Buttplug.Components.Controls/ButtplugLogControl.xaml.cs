// <copyright file="ButtplugLogControl.xaml.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Buttplug.Core;
using Microsoft.Win32;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Buttplug.Components.Controls
{
    public class LogList : ObservableCollection<string>
    {
    }

    [Target("ButtplugGUILogger")]
    public sealed class ButtplugGUIMessageNLogTarget : TargetWithLayoutHeaderAndFooter
    {
        private readonly LogList _logs;
        private readonly object _logLock = new object();
        public long MaxLogs;

        public ButtplugGUIMessageNLogTarget(LogList aList, long aMaxLogs = 1000)
        {
            _logs = aList;
            MaxLogs = aMaxLogs;
            BindingOperations.EnableCollectionSynchronization(_logs, _logLock);
        }

        protected override void Write(LogEventInfo aLogEvent)
        {
            _logs.Add(Layout.Render(aLogEvent));
            while (_logs.Count > MaxLogs)
            {
                _logs.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ButtplugLogControl : IDisposable
    {
        private readonly LogList _logs;
        private readonly ButtplugGUIMessageNLogTarget _logTarget;
        private LoggingRule _outgoingLoggingRule;

        public long MaxLogs
        {
            get
            {
                return _logTarget.MaxLogs;
            }

            set
            {
                _logTarget.MaxLogs = value;
            }
        }

        public ButtplugLogControl()
        {
            var c = LogManager.Configuration ?? new LoggingConfiguration();
            _logs = new LogList();

            // Null check Dispatcher, otherwise test bringup for GUI tests will fail.
            if (Dispatcher != null)
            {
                _logTarget = new ButtplugGUIMessageNLogTarget(_logs);
                c.AddTarget("ButtplugGuiLogger", _logTarget);
                _outgoingLoggingRule = new LoggingRule("*", LogLevel.Debug, _logTarget);
                c.LoggingRules.Add(_outgoingLoggingRule);
                LogManager.Configuration = c;
            }

            InitializeComponent();
            LogLevelComboBox.SelectionChanged += LogLevelSelectionChangedHandler;
            LogListBox.ItemsSource = _logs;
        }

        private void Dispose(bool aDisposing)
        {
            if (aDisposing)
            {
                _logTarget?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string[] GetLogs()
        {
            return _logs.ToArray();
        }

        private void SaveLogFileButton_Click(object aSender, RoutedEventArgs aEvent)
        {
            var dialog = new SaveFileDialog
            {
                CheckFileExists = false,
                CheckPathExists = true,
                OverwritePrompt = true,
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var sw = new System.IO.StreamWriter(dialog.FileName, false);
            foreach (var line in _logs.ToList())
            {
                sw.WriteLine(line);
            }

            sw.Close();
        }

        private void LogLevelSelectionChangedHandler(object aSender, SelectionChangedEventArgs aEvent)
        {
            var c = LogManager.Configuration;
            var level = ((ComboBoxItem)LogLevelComboBox.SelectedValue).Content.ToString();
            try
            {
                c.LoggingRules.Remove(_outgoingLoggingRule);
                _outgoingLoggingRule = new LoggingRule("*", LogLevel.FromString(level), _logTarget);
                c.LoggingRules.Add(_outgoingLoggingRule);
                LogManager.Configuration = c;
            }
            catch (ArgumentException)
            {
                LogManager.GetCurrentClassLogger().Error($"Log Level \"{level}\" is not a valid log level!");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _logs.Clear();
        }

        private void LogListBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender != LogListBox)
            {
                return;
            }

            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                var builder = new StringBuilder();
                foreach (var item in LogListBox.SelectedItems)
                {
                    if (item is string)
                    {
                        builder.AppendLine(item as string);
                    }
                    else if (item is ListBoxItem)
                    {
                        builder.AppendLine((item as ListBoxItem).Content as string);
                    }
                }

                try
                {
                    Clipboard.SetText(builder.ToString());
                }
                catch (Exception ex)
                {
                    // We've seen weird instances of can't open clipboard
                    // but it's pretty rare. Log it.
                    var logMan = new ButtplugLogManager();
                    var log = logMan.GetLogger(GetType());
                    log.LogException(ex);
                }
            }
        }
    }
}