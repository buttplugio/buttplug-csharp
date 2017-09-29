using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.DeviceSimulator.PipeMessages;

namespace Buttplug.Server.Managers.SimulatorManager
{
    public class SimulatorManager : DeviceSubtypeManager
    {
        private NamedPipeServerStream _pipeServer;

        private Task _readThread;

        private CancellationTokenSource _tokenSource;

        private bool _scanning;

        private PipeMessageParser _parser;

        private ButtplugLogManager _logManager;

        public SimulatorManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            BpLogger.Info("Loading Simulator Manager");
            _scanning = false;

            _parser = new PipeMessageParser();

            _logManager = new ButtplugLogManager();

            _pipeServer = new NamedPipeServerStream("ButtplugDeviceSimulator", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

            _tokenSource = new CancellationTokenSource();
            _readThread = new Task(() => { connAccepter(_tokenSource.Token); }, _tokenSource.Token, TaskCreationOptions.LongRunning);
            _readThread.Start();
        }

        internal void Vibrate(SimulatedButtplugDevice aDev, double aSpeed)
        {
            if (_pipeServer.IsConnected)
            {
                var msg = _parser.Serialize(new Vibrate(aDev.Identifier, aSpeed));
                _pipeServer.Write(Encoding.ASCII.GetBytes(msg), 0, msg.Length);
            }
        }

        internal void StopDevice(SimulatedButtplugDevice aDev)
        {
            if (_pipeServer.IsConnected)
            {
                var msg = _parser.Serialize(new StopDevice(aDev.Identifier));
                _pipeServer.Write(Encoding.ASCII.GetBytes(msg), 0, msg.Length);
            }
        }

        private void connAccepter(CancellationToken aCancellationToken)
        {
            while (!aCancellationToken.IsCancellationRequested)
            {
                if (!_pipeServer.IsConnected)
                {
                    _scanning = false;
                    _pipeServer.WaitForConnection();
                    pipeReader(aCancellationToken);
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        private void pipeReader(CancellationToken aCancellationToken)
        {
            while (!aCancellationToken.IsCancellationRequested && _pipeServer.IsConnected)
            {
                var buffer = new byte[4096];
                string msg = string.Empty;
                var len = -1;
                while (len < 0 || (len == buffer.Length && buffer[4095] != '\0'))
                {
                    var waiter = _pipeServer.ReadAsync(buffer, 0, buffer.Length);
                    while (!waiter.GetAwaiter().IsCompleted)
                    {
                        if (!_pipeServer.IsConnected)
                        {
                            return;
                        }

                        Thread.Sleep(100);
                    }

                    len = waiter.GetAwaiter().GetResult();

                    if (len > 0)
                    {
                        msg += Encoding.ASCII.GetString(buffer, 0, len);
                    }
                }

                switch (_parser.Deserialize(msg))
                {
                    case FinishedScanning fs:
                        InvokeScanningFinished();
                        _scanning = false;
                        break;

                    case DeviceAdded da:
                        InvokeDeviceAdded(new DeviceAddedEventArgs(new SimulatedButtplugDevice(this, _logManager, da.Name, da.Id)));
                        break;

                    case DeviceRemoved dr:
                        //InvokeDevice (new DeviceAddedEventArgs(new SimulatedButtplugDevice(_logManager, "Test", "1234")));
                        break;

                    default:
                        break;
                }
            }
        }

        public override void StartScanning()
        {
            BpLogger.Info("SimulatorManager start scanning");
            if (_pipeServer.IsConnected)
            {
                _scanning = true;
                var msg = _parser.Serialize(new StartScanning());
                _pipeServer.Write(Encoding.ASCII.GetBytes(msg), 0, msg.Length);
            }
        }

        public override void StopScanning()
        {
            BpLogger.Info("SimulatorManager stop scanning");
            if (_pipeServer.IsConnected)
            {
                _scanning = false;
                var msg = _parser.Serialize(new StopScanning());
                _pipeServer.Write(Encoding.ASCII.GetBytes(msg), 0, msg.Length);
            }
        }

        public override bool IsScanning()
        {
            return _scanning;
        }
    }
}