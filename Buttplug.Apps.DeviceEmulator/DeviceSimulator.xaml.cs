using System.Windows;
using System.Windows.Controls;

namespace Buttplug.Apps.DeviceEmulator
{
    /// <summary>
    /// Interaction logic for DeviceSimulation.xaml
    /// </summary>
    public partial class DeviceSimulator
    {
        private int tabId;

        public DeviceSimulator(int aTabId)
        {
            InitializeComponent();
            tabId = aTabId;
            DeviceName.Text = "Device" + tabId;
            DeviceId.Text = DeviceName.Text;
        }
    }
}
