using System;
using System.Windows;

namespace Buttplug.Apps.KiirooEmulatorGUI
{
    public partial class KiirooEmulatorPanel
    {
        public event EventHandler<bool> ServerStatusChanged;

        public KiirooEmulatorPanel()
        {
            InitializeComponent();
        }

        private void StartServer()
        {
            ServerStatusChanged?.Invoke(this, true);
            ServerButton.Content = "Stop Server";
        }

        private async void StopServer()
        {
            ServerStatusChanged?.Invoke(this, false);
            await Dispatcher.InvokeAsync(() =>
            {
                ServerButton.Content = "Start Server";
            });
        }

        private void ServerButton_Click(object sender, RoutedEventArgs e)
        {
            if ((string)ServerButton.Content == "Start Server")
            {
                StartServer();
            }
            else
            {
                StopServer();
            }
        }
    }
}