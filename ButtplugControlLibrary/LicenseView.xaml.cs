using Buttplug.Core;

namespace ButtplugControlLibrary
{
    /// <summary>
    /// Interaction logic for LicenseView.xaml
    /// </summary>
    public partial class LicenseView
    {
        public LicenseView()
        {
            InitializeComponent();
            LicenseText.Text = ButtplugService.GetLicense();
        }
    }
}
