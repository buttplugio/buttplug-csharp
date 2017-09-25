using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Buttplug.Apps.DeviceEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private TcpClient _socket;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Connect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                _socket = new TcpClient();
                _socket.Connect("localhost", 54321);
            }
            catch
            {
                if (_socket.Connected)
                {
                    _socket.Close();
                }
                _socket = null;
            }
        }

        private void AddDevice_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var tab = new TabItem();
            tab.Content = new DeviceSimulator(DeviceTabs.Items.Count);
            tab.Header = (tab.Content as DeviceSimulator).DeviceName.Text;
            DeviceTabs.Items.Add(tab);
            DeviceTabs.SelectedIndex = DeviceTabs.Items.Count - 1;
        }
        
    }
}
