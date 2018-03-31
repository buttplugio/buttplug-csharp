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

    public partial class VibeConfigTab
    {
        public event EventHandler<double> MultiplierChanged;
        private readonly Logger _log;
        public VibeConfigTab()
        {
            _log = LogManager.GetCurrentClassLogger();
            InitializeComponent();
        }

        private void multiplierSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            MultiplierChanged?.Invoke(this, e.NewValue);
        }
    }
}