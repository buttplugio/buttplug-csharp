using System.Windows;

namespace ButtplugKiirooEmulatorGUI
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
            ButtplugTab.InitializeButtplugServer();
            var emu = new KiirooEmulatorPanel(ButtplugTab.BpServer);
            ButtplugTab.SetApplicationTab("Kiiroo Emulator", emu);
            emu.StartServer();
        }
    }
}
