using System;
using System.Collections.Generic;
using System.Linq;
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
using Buttplug.Core;
using ButtplugWebsockets;

namespace ButtplugServerGUI
{
    /// <summary>
    /// Interaction logic for WebsocketServerControl.xaml
    /// </summary>
    public partial class WebsocketServerControl : UserControl
    {
        private ButtplugWebsocketServer _ws;

        public WebsocketServerControl()
        {
            InitializeComponent();
            _ws = new ButtplugWebsocketServer();
            
        }

        public void StartServer(ButtplugService aService)
        {
            _ws.StartServer(aService);
        }
    }
}
