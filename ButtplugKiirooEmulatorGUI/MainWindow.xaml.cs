using System;
using System.ComponentModel;
using System.Windows;

namespace ButtplugKiirooEmulatorGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly KiirooEmulatorPanel _emu;

        public MainWindow()
        {
            InitializeComponent();
            if (Application.Current == null)
            {
                return;
            }

            ButtplugTab.SetServerDetails("Kiiroo Emulator", 0);
            _emu = new KiirooEmulatorPanel(ButtplugTab.GetService());
            ButtplugTab.SetApplicationTab("Kiiroo Emulator", _emu);
            Closing += ClosingHandler;
            _emu.StartServer();
        }

        private void ClosingHandler(object aObj, CancelEventArgs e)
        {
            _emu?.StopServer();
        }
    }
}
