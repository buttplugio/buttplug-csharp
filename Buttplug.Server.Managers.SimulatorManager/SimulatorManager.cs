using System.Collections.Concurrent;
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
        private Task _writeThread;
        private Task _pingThread;

        private CancellationTokenSource _tokenSource;

        private bool _scanning;

        private PipeMessageParser _parser;

        private ButtplugLogManager _logManager;

        private ConcurrentQueue<IDeviceSimulatorPipeMessage> _msgQueue = new ConcurrentQueue<IDeviceSimulatorPipeMessage>();

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
            _writeThread = new Task(() => { pipeWriter(_tokenSource.Token); }, _tokenSource.Token, TaskCreationOptions.LongRunning);
            _pingThread = new Task(() => { pingWriter(_tokenSource.Token); }, _tokenSource.Token, TaskCreationOptions.LongRunning);

            _readThread.Start();
            _writeThread.Start();
            _pingThread.Start();
        }

        internal void Vibrate(SimulatedButtplugDevice aDev, double aSpeed, uint aVibratorId)
        {
            _msgQueue.Enqueue(new Vibrate(aDev.Identifier, aSpeed, aVibratorId));
        }

        internal void Rotate(SimulatedButtplugDevice aDev, uint aSpeed, bool aClockwise)
        {
            _msgQueue.Enqueue(new Rotate(aDev.Identifier, aSpeed, aClockwise));
        }

        internal void StopDevice(SimulatedButtplugDevice aDev)
        {
            _msgQueue.Enqueue(new StopDevice(aDev.Identifier));
        }

        internal void Linear(SimulatedButtplugDevice aDev, double aSpeed, double aPosition)
        {
            _msgQueue.Enqueue(new Linear(aDev.Identifier, aSpeed, aPosition));
        }

        internal void Linear2(SimulatedButtplugDevice aDev, uint aDuration, double aPosition)
        {
            _msgQueue.Enqueue(new Linear2(aDev.Identifier, aDuration, aPosition));
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
                    _pipeServer.Disconnect();
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        private void pingWriter(CancellationToken aCancellationToken)
        {
            while (!aCancellationToken.IsCancellationRequested)
            {
                if (_pipeServer.IsConnected && _msgQueue.IsEmpty)
                {
                    _msgQueue.Enqueue(new Ping());
                    Thread.Sleep(100);
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
                    try
                    {
                        var waiter = _pipeServer.ReadAsync(buffer, 0, buffer.Length);
                        while (!waiter.GetAwaiter().IsCompleted)
                        {
                            if (!_pipeServer.IsConnected)
                            {
                                return;
                            }

                            Thread.Sleep(10);
                        }

                        len = waiter.GetAwaiter().GetResult();
                        if (len > 0)
                        {
                            msg += Encoding.ASCII.GetString(buffer, 0, len);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                switch (_parser.Deserialize(msg))
                {
                    case FinishedScanning fs:
                        BpLogger.Info("SimulatorManager recieved stop scanning");
                        _scanning = false;
                        InvokeScanningFinished();
                        break;

                    case DeviceAdded da:
                        InvokeDeviceAdded(new DeviceAddedEventArgs(new SimulatedButtplugDevice(this, _logManager, da)));
                        break;

                    case DeviceRemoved dr:
                        // InvokeDevice (new DeviceAddedEventArgs(new SimulatedButtplugDevice(_logManager, "Test", "1234")));
                        break;

                    default:
                        break;
                }
            }
        }

        private void pipeWriter(CancellationToken aCancellationToken)
        {
            while (!aCancellationToken.IsCancellationRequested)
            {
                if (_pipeServer.IsConnected && _msgQueue.TryDequeue(out IDeviceSimulatorPipeMessage msg))
                {
                    var str = _parser.Serialize(msg);
                    if (str != null)
                    {
                        try
                        {
                            _pipeServer.Write(Encoding.ASCII.GetBytes(str), 0, str.Length);
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        public override void StartScanning()
        {
            if (!_pipeServer.IsConnected)
            {
                BpLogger.Info("SimulatorManager can't start scanning until connected");
                return;
            }

            _scanning = true;
            BpLogger.Info("SimulatorManager start scanning");
            _msgQueue.Enqueue(new StartScanning());
        }

        public override void StopScanning()
        {
            _scanning = false;
            BpLogger.Info("SimulatorManager stop scanning");
            _msgQueue.Enqueue(new StopScanning());
        }

        public override bool IsScanning()
        {
            return _scanning;
        }
    }
}