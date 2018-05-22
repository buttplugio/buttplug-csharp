using System;
using System.Threading.Tasks;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Controls;

namespace Buttplug.Apps.GameVibrationRouter.GUI
{
    /// <summary>
    /// Interaction logic for VibeGraphTab.xaml
    /// </summary>
    public partial class VibeGraphTab : UserControl
    {   /// <summary>
        /// Interaction logic for VibrationGraphTab.xaml
        /// </summary>

        // Both of these members need to be public, otherwise
        // livecharts can't see them to chart them.
        // ReSharper disable once MemberCanBePrivate.Global
        public SeriesCollection LowPowerSeriesCollection { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public SeriesCollection HighPowerSeriesCollection { get; set; }
        private readonly ChartValues<double> LowPowerValues;
        private readonly ChartValues<double> HighPowerValues;
        public event EventHandler<double> MultiplierChanged;
        public event EventHandler<bool> PassthruChanged;

        public VibeGraphTab()
        {
            InitializeComponent();
            LowPowerValues = new ChartValues<double>();
            HighPowerValues = new ChartValues<double>();
            for (var i = 0; i < 200; ++i)
            {
                LowPowerValues.Add(0);
                HighPowerValues.Add(0);
            }
            LowPowerSeriesCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Low Power Vibrator",
                        Values = LowPowerValues,
                        PointGeometrySize = 0
                    }
                };
            HighPowerSeriesCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "High Power Vibrator",
                        Values = HighPowerValues,
                        PointGeometrySize = 0
                    }
                };
            DataContext = this;
            // The automated updater doesn't seem to run correctly after it loses focus, so we'll 
            // manually run update steps ourselves.
            Dispatcher.Invoke(() =>
            {
                HighPowerChart.UpdaterState = UpdaterState.Paused;
                LowPowerChart.UpdaterState = UpdaterState.Paused;
            });

        }

        public void AddVibrationValue(double aLowPower, double aHighPower)
        {
            LowPowerValues.RemoveAt(0);
            LowPowerValues.Add(aLowPower);
            HighPowerValues.RemoveAt(0);
            HighPowerValues.Add(aHighPower);
            // Manually run chart update
            try
            {
                Dispatcher.Invoke(() =>
                {
                    LowPowerChart.Update(false, true);
                    HighPowerChart.Update(false, true);
                });
            }
            catch (TaskCanceledException)
            {
                // Usually means we're shutting down. noop.
            }
        }

        private void PassthruCheckBox_Changed(object aSender, RoutedEventArgs aArgs)
        {
            PassthruChanged?.Invoke(this, PassthruCheckBox.IsChecked.Value);
        }

        private void multiplierSlider_ValueChanged(object aSender, System.Windows.RoutedPropertyChangedEventArgs<double> aArgs)
        {
            MultiplierChanged?.Invoke(this, aArgs.NewValue);
        }
    }
}