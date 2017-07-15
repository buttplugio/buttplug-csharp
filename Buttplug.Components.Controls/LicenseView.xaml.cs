using Buttplug.Server;

namespace Buttplug.Components.Controls
{
    /// <summary>
    /// Interaction logic for LicenseView.xaml
    /// </summary>
    public partial class LicenseView
    {
        public LicenseView()
        {
            InitializeComponent();
            LicenseText.Text = ButtplugServer.GetLicense();
        }
    }
}
