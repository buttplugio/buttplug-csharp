using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Buttplug.Core;

namespace Buttplug.Components.Controls
{
    /// <summary>
    /// Interaction logic for ButtplugAboutControl.xaml
    /// </summary>
    public partial class ButtplugAboutControl
    {
        private string _gitHash;
        private string _buildType;
        private uint _clickCounter;

        public event EventHandler AboutImageClickedABunch;

        public ButtplugAboutControl()
        {
            InitializeComponent();
        }

        public void InitializeVersion()
        {
            AboutVersionNumber.Text = Assembly.GetEntryAssembly().GetName().Version.ToString();
            var longVer = System.Diagnostics.FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location)
                .ProductVersion;
            if (longVer.Length > 0)
            {
                AboutVersionNumber.Text = longVer;
            }

            // AssemblyInformationalVersion("1.0.0.0-dev")

            var pos = longVer.IndexOf('-');
            if (pos > 0)
            {
                _buildType = longVer.Substring(pos);
            }

            AboutGitVersion.Text = string.Empty;
            var attribute = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyGitVersion), false)[0];
            if (attribute != null && ((AssemblyGitVersion)attribute).Value.Length > 0)
            {
                _gitHash = ((AssemblyGitVersion)attribute).Value;
                AboutGitVersion.Text = _gitHash;
                AboutGitVersion.MouseDown += GithubRequestNavigate;
            }
        }

        public string GetAboutVersion()
        {
            return AboutVersionNumber.Text + " " + _gitHash;
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
