using Buttplug.Core;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ButtplugControlLibrary
{
    /// <summary>
    /// Interaction logic for LicenseView.xaml
    /// </summary>
    public partial class LicenseView : Window
    {
        public LicenseView()
        {
            InitializeComponent();
            ((TextBox)LicenseText).Text = ButtplugService.GetLicense();
        }
    }
}
