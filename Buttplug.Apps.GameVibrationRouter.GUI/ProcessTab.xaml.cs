using EasyHook;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Buttplug.Apps.GameVibrationRouter.GUI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>

    public partial class ProcessTab
    {
        public class ProcessInfo
        {
            public String FileName;
            public Int32 Id;
            public String Owner;

            public override string ToString()
            {
                var f = System.IO.Path.GetFileNameWithoutExtension(FileName);
                return $"{f} ({Id})";
            }
        }

        private class ProcessInfoList : ObservableCollection<ProcessInfo>
        {
        }

        public bool Attached
        {
            set
            {
                _attached = value;
                ProcessListBox.IsEnabled = !value;
                RefreshButton.IsEnabled = !value;
                AttachButton.IsEnabled = true;
                AttachButton.Content = value ? "Detach From Process" : "Attach To Process";
            }
        }

        public string ProcessError
        {
            set { ErrorLabel.Content = value; }
        }

        private readonly ProcessInfoList _processList = new ProcessInfoList();

        public event EventHandler<int> ProcessAttachRequested;

        public event EventHandler<bool> ProcessDetachRequested;

        private bool _attached;
        private readonly Logger _log;

        public ProcessTab()
        {
            _log = LogManager.GetCurrentClassLogger();
            InitializeComponent();
            ProcessListBox.ItemsSource = _processList;
            ProcessListBox.SelectionChanged += OnSelectionChanged;
            //EnumProcesses();
        }

        private void EnumProcesses()
        {
            _processList.Clear();
            foreach (var currentProc in from proc in Process.GetProcesses() orderby proc.ProcessName select proc)
            {
                try
                {
                    // This can sometimes happen between calling GetProcesses and getting here. Save ourselves the throw.
                    if (currentProc.HasExited)
                    {
                        continue;
                    }
                    _processList.Add(new ProcessInfo
                    {
                        FileName = currentProc.ProcessName,
                        Id = currentProc.Id,
                        Owner = RemoteHooking.GetProcessIdentity(currentProc.Id).Name
                    });
                }
                catch (AccessViolationException)
                {
                    // noop, there's a lot of system processes we can't see.
                }
                catch (Win32Exception)
                {
                    // noop, there's a lot of system processes we can't see.
                }
                catch (Exception aEx)
                {
                    _log.Error(aEx);
                }
            }
        }

        private void OnSelectionChanged(object aObj, EventArgs aEvent)
        {
            AttachButton.IsEnabled = ProcessListBox.SelectedItems.Count == 1;
        }

        private void AttachButton_Click(object aObj, System.Windows.RoutedEventArgs aEvent)
        {
            AttachButton.IsEnabled = false;
            RefreshButton.IsEnabled = false;
            if (!_attached)
            {
                var process = ProcessListBox.SelectedItems.Cast<ProcessInfo>().ToList();
                ProcessAttachRequested?.Invoke(this, process[0].Id);
            }
            else
            {
                ProcessDetachRequested?.Invoke(this, true);
            }
        }

        private void RefreshButton_Click(object aObj, System.Windows.RoutedEventArgs aEvent)
        {
            EnumProcesses();
        }
    }
}