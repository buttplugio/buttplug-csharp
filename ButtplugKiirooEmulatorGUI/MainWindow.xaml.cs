using System.Windows;

namespace ButtplugKiirooEmulatorGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly KiirooEmulatorPanel _emu;
        public MainWindow()
        {
            InitializeComponent();
            if (Application.Current == null)
            {
                return;
            }
            ButtplugTab.InitializeButtplugServer("Kiiroo Emulator", 100);
            _emu = new KiirooEmulatorPanel(ButtplugTab.BpServer);
            ButtplugTab.SetApplicationTab("Kiiroo Emulator", _emu);
            _emu.StartServer();
        }
    }
}
