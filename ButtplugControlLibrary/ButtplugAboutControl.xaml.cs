using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ButtplugControlLibrary
{
    /// <summary>
    /// Interaction logic for ButtplugAboutControl.xaml
    /// </summary>
    public partial class ButtplugAboutControl : UserControl
    {
        private string _gitHash;
        private uint _clickCounter;
        public event EventHandler AboutImageClickedABunch;
        public ButtplugAboutControl()
        {
            InitializeComponent();   
        }

        public void InitializeVersion()
        {
            AboutVersionNumber.Text = Assembly.GetEntryAssembly().GetName().Version.ToString();
            _gitHash = System.Diagnostics.FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location)
                .ProductVersion;
            if (_gitHash.Length > 0)
            {
                AboutVersionNumber.Text += $"-{_gitHash.Substring(0, 8)}";
                AboutVersionNumber.MouseDown += GithubRequestNavigate;
            }
        }

        public string GetAboutVersion()
        {
            return AboutVersionNumber.Text;
        }

        private void GithubRequestNavigate(object o, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(
                new Uri($"http://github.com/metafetish/buttplug-csharp/commit/{_gitHash}").AbsoluteUri);
        }

        private void PatreonRequestNavigate(object o, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(new Uri("http://patreon.com/qdot").AbsoluteUri);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void IconImage_Click(object sender, RoutedEventArgs e)
        {
            _clickCounter += 1;
            if (_clickCounter < 5)
            {
                return;
            }
            IconImage.MouseDown -= IconImage_Click;
            AboutImageClickedABunch?.Invoke(this, e);
        }
    }
}
