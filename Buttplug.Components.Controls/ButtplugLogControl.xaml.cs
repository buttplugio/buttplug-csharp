﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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
        private readonly Thread _winThread;
        public long MaxLogs;

        public ButtplugGUIMessageNLogTarget(LogList aList, Thread aWinThread, long aMaxLogs = 1000)
        {
            // TODO This totally needs a mutex or something
            _logs = aList;
            _winThread = aWinThread;
            MaxLogs = aMaxLogs;
        }

        protected override void Write(LogEventInfo aLogEvent)
        {
            try
            {
                Dispatcher.FromThread(_winThread).Invoke(() =>
                {
                    _logs.Add(Layout.Render(aLogEvent));
                    while (_logs.Count > MaxLogs)
                    {
                        _logs.RemoveAt(0);
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // noop
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
                _logTarget = new ButtplugGUIMessageNLogTarget(_logs, Dispatcher.Thread);
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

            if ((e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
                && e.Key == Key.C)
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

                Clipboard.SetText(builder.ToString());
            }
        }
    }
}
