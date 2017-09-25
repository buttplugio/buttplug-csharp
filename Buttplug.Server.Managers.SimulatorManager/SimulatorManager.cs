
using Buttplug.Core;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Buttplug.Server.Managers.SimulatorManager
{
    public class SimulatorManager : DeviceSubtypeManager
    {
        private TcpListener _socket;

        private Task _readThread;

        private CancellationTokenSource _tokenSource;

        public SimulatorManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            BpLogger.Info("Loading Simulator Manager");
            _socket = new TcpListener(new IPAddress(new byte[] { 127, 0, 0, 1 }), 54321);
            _socket.Start();

            _tokenSource = new CancellationTokenSource();
            _readThread = new Task(() => { connAccepter(_tokenSource.Token); }, _tokenSource.Token, TaskCreationOptions.LongRunning);
            _readThread.Start();
        }

        private void connAccepter(CancellationToken aCancellationToken)
        {
            if (_socket.Pending())
            {
                var client = _socket.AcceptTcpClient();
            }
            else
            {
                Thread.Sleep(500);
            }
        }

        public override void StartScanning()
        {
            BpLogger.Info("SimulatorManager start scanning");
        }

        public override void StopScanning()
        {
            BpLogger.Info("SimulatorManager stop scanning");
        }

        public override bool IsScanning()
        {
            return false;
        }
    }
}