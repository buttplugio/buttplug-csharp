using System.Windows;

namespace Buttplug.Apps.ExampleClientGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (Application.Current == null)
            {
                return;
            }

            /*ButtplugTab.SetServerDetails("Kiiroo Emulator", 0);
            InitializeComponent();
            var emu = new ClientTestPanel();
            ButtplugTab.SetApplicationTab("Kiiroo Emulator", emu);*/
        }
    }
}
