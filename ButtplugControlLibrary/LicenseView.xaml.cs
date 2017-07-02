using Buttplug.Core;
using System.Windows.Controls;

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
            ((TextBox)LicenseText).Text = ButtplugService.GetLicense();
        }
    }
}
