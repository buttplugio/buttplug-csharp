using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using Buttplug.Core;
using JetBrains.Annotations;

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
                foreach (string port in comPortsToScan)
                {
                    BpLogger.Info("Scanning "+port);

                    SerialPort serialPort = new SerialPort(port);

                    serialPort.ReadTimeout = 200;
                    serialPort.WriteTimeout = 200;
                    serialPort.BaudRate = 19200;
                    serialPort.Parity = Parity.None;
                    serialPort.StopBits = StopBits.One;
                    serialPort.DataBits = 8;
                    serialPort.Handshake = Handshake.None;

                    try
                    {
                        serialPort.Open();
                    }
                    catch
                    {
                        // This port is inaccessible.
                        // Possibly because a device detected earlier is already using it.
                        continue;
                    }

                    // We send 0x00 up to 11 times until we get 0x07 back.
                    // Why 11? See et312-protocol.org
                    var detected = false;

                    for (int i = 0; i < 11; i++)
                    {
                        serialPort.Write(new byte[] { 0x00 }, 0, 1);

                        try
                        {
                            if (serialPort.ReadByte() == 0x07)
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
                        catch
                        {
                            // Can't read from this port? Stop trying.
                            break;
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
                System.Threading.Thread.Sleep(5000);
                _isScanning = false;
            }

            InvokeScanningFinished();
        }
    }
}