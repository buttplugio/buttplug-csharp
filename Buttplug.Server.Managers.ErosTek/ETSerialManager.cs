using System;
using System.IO.Ports;
using System.Threading;
using Buttplug.Core;
using static Buttplug.Server.Managers.ETSerialManager.ET312Protocol;

namespace Buttplug.Server.Managers.ETSerialManager
{
    public class ETSerialManager : DeviceSubtypeManager
    {
        private bool _isScanning = false;
        private Thread _scanThread;
        private string[] _searchComPorts;

        public ETSerialManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            BpLogger.Info("Loading ErosTek Serial Port Manager");
        }

        public override void StartScanning()
        {
            BpLogger.Info("Starting Scanning Serial Ports for ErosTek Devices");
            _scanThread = new Thread(() => ScanSerialPorts(_searchComPorts));
            _scanThread.Start();
        }

        public override void StopScanning()
        {
            BpLogger.Info("Stopping Scanning Serial Ports for ErosTek Devices");
            _isScanning = false;
        }

        public override bool IsScanning()
        {
            return _isScanning;
        }

        private void ScanSerialPorts(string[] selectedComPorts)
        {
            string[] comPortsToScan;
            _isScanning = true;

            while (_isScanning)
            {
                // Enumerate Ports
                if (selectedComPorts == null || selectedComPorts.Length == 0)
                {
                    comPortsToScan = SerialPort.GetPortNames();
                }
                else
                {
                    comPortsToScan = selectedComPorts;
                }

                // try to detect devices on all port
                foreach (var port in comPortsToScan)
                {
                    BpLogger.Info("Scanning " + port);

                    SerialPort serialPort = new SerialPort(port)
                    {
                        ReadTimeout = 200,
                        WriteTimeout = 200,
                        BaudRate = 19200,
                        Parity = Parity.None,
                        StopBits = StopBits.One,
                        DataBits = 8,
                        Handshake = Handshake.None,
                    };

                    try
                    {
                        serialPort.Open();
                    }
                    catch (Exception ex)
                    {
                        if (ex is UnauthorizedAccessException
                            || ex is ArgumentOutOfRangeException
                            || ex is System.IO.IOException)
                        {
                            // This port is inaccessible.
                            // Possibly because a device detected earlier is already using it,
                            // or because our required parameters are not supported
                            continue;
                        }

                        throw ex;
                    }

                    // We send 0x00 up to 11 times until we get 0x07 back.
                    // Why 11? See et312-protocol.org
                    var detected = false;

                    for (int i = 0; i < 11; i++)
                    {
                        try
                        {
                            serialPort.Write(new byte[] { (byte)SerialCommand.Sync }, 0, 1);
                        }
                        catch (TimeoutException)
                        {
                            // Can't write to this port? Skip to the next one.
                            break;
                        }

                        try
                        {
                            if (serialPort.ReadByte() == (byte)SerialResponse.Error)
                            {
                                detected = true;
                                break;
                            }
                        }
                        catch (TimeoutException)
                        {
                            // No response? Keep trying.
                            continue;
                        }
                    }

                    if (detected)
                    {
                        // This seems to be an ET312! Let's try to create the device.
                        try
                        {
                            var device = new ET312Device(serialPort, LogManager, "Erostek ET-312", port);

                            BpLogger.Info("Found device at port " + port);

                            // Device succesfully created!
                            InvokeDeviceAdded(new DeviceAddedEventArgs(device));
                            continue;
                        }
                        catch (ET312HandshakeException)
                        {
                            // Sync Failed. Not the device we were expecting or comms garbled.
                        }
                    }

                    // No useful device detected on this port. Close the port.
                    serialPort.Close();
                    serialPort.Dispose();
                }

                System.Threading.Thread.Sleep(3000);

                // _isScanning = false; // Uncomment to disable continuuous serial port scanning
            }

            InvokeScanningFinished();
        }
    }
}