using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Buttplug.Components.Controls
{
    /// <summary>
    /// Interaction logic for ButtplugAboutControl.xaml
    /// </summary>
    public partial class ButtplugAboutControl
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

        private void GithubRequestNavigate(object aObj, MouseButtonEventArgs aEvent)
        {
            System.Diagnostics.Process.Start(
                new Uri($"http://github.com/metafetish/buttplug-csharp/commit/{_gitHash}").AbsoluteUri);
        }

        private void PatreonRequestNavigate(object aObj, MouseButtonEventArgs aEvent)
        {
            System.Diagnostics.Process.Start(new Uri("http://patreon.com/qdot").AbsoluteUri);
        }

        private void Hyperlink_RequestNavigate(object aSender, System.Windows.Navigation.RequestNavigateEventArgs aEvent)
        {
            System.Diagnostics.Process.Start(aEvent.Uri.AbsoluteUri);
        }

        private void IconImage_Click(object aSender, RoutedEventArgs aEvent)
        {
            _clickCounter += 1;
            if (_clickCounter < 5)
            {
                return;
            }

            IconImage.MouseDown -= IconImage_Click;
            AboutImageClickedABunch?.Invoke(this, aEvent);
        }

        private void LicenseHyperlink_Click(object aSender, RoutedEventArgs aEvent)
        {
            new LicenseView().Show();
        }
    }
}
