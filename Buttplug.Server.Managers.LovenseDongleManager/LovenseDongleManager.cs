// <copyright file="LovenseDongleManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Devices.Configuration;
using HidSharp;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Buttplug.Server.Managers.LovenseDongleManager
{
    public class LovenseDongleConnectionException : Exception
    { }

    public class LovenseDongleManager : TimedScanDeviceSubtypeManager
    {
        private SerialStream _dongleStream;
        private readonly Mutex _sendMute = new Mutex();
        private readonly SemaphoreSlim _receiveMute = new SemaphoreSlim(0, 1);
        private bool _runScan = false;

        private readonly Dictionary<string, LovenseDongleDeviceImpl> _devices = new Dictionary<string, LovenseDongleDeviceImpl>();
        private ButtplugDeviceFactory _deviceFactory;
        private readonly FixedSizedQueue<string> _sendData = new FixedSizedQueue<string>(2);
        private readonly JsonSerializer _serializer = new JsonSerializer();

        public LovenseDongleManager(IButtplugLogManager aLogManager)
              : base(aLogManager)
        {
        }

        protected override void RunScan()
        {
            // TODO Can the dongle not scan while it's connected to something? Seriously?
            if (_devices.Count > 0)
            {
                return;
            }

            if (_dongleStream == null)
            {
                try
                {
                    ScanForDongle();
                }
                catch (LovenseDongleConnectionException aEx)
                {

                }
            }

            _runScan = true;
            BpLogger.Info("LovenseDongleManager starts scanning");
        }

        private void ScanForDongle()
        {
            BpLogger.Info("Scanning for Lovense Dongle");
            SerialDevice devPort = null;

            foreach (var port in DeviceList.Local.GetSerialDevices())
            {
                // TODO Deal with systems with multiple dongles attached?
                // This is the USB device name for the Lovense Dongle
                if (!port.GetFriendlyName().Contains("USB-SERIAL CH340"))
                {
                    continue;
                }

                devPort = port;

                // Once we've found one, break, otherwise we may encounter a system with multiple dongles.
                break;
            }

            BpLogger.Debug($"Found Lovense Dongle: {devPort.GetFriendlyName()}");

            var config = new OpenConfiguration();
            config.SetOption(OpenOption.Exclusive, true);
            config.SetOption(OpenOption.Interruptible, true);
            config.SetOption(OpenOption.TimeoutIfInterruptible, 1000);
            config.SetOption(OpenOption.TimeoutIfTransient, 1000);

            if (!devPort.TryOpen(config, out _dongleStream))
            {
                _dongleStream = null;
                return;
            }

            _dongleStream.BaudRate = 115200;
            _dongleStream.DataBits = 8;
            _dongleStream.StopBits = 1;
            _dongleStream.Parity = SerialParity.None;

            if (!_dongleStream.CanRead || !_dongleStream.CanWrite)
            {
                _dongleStream.Close();
                _dongleStream = null;
                return;
            }

            // Dongle responds with only \n, use it as line break. But commands to dongle should be sent as \r\n
            _dongleStream.NewLine = "\n";

            // Detect dongle
            _dongleStream.WriteTimeout = 500;
            _dongleStream.ReadTimeout = 500;

            try
            {
                // Send a Lovense style device type query, which will return a "D" toy code.
                var reply = SendAndExpectReply("DeviceType;\r");
                // Expect back a Lovense device type status, D:XXX:YYYYYYYYY
                // D - static, device code
                // X - dongle firmware version
                // Y - dongle bluetooth radio ID
                var components = reply.Split(':');
                BpLogger.Debug($"Lovense Dongle status return: ${reply}");
                if (components.Length != 3)
                {
                    throw new Exception("Lovense Dongle Component Length not 3.");
                }
                BpLogger.Info($"Lovense Dongle Version: {components[1]}");
            }
            catch (Exception ex)
            {
                BpLogger.Error($"Exception while checking Lovense Dongle: {ex.ToString()}");
                _dongleStream.Close();
                _dongleStream = null;
                return;
            }

            _dongleStream.Closed += _dongleStream_Closed;

            var result = SendAndExpectSuccess(new LovenseDongleOutgoingMessage()
            {
                Type = LovenseDongleOutgoingMessage.MessageType.USB,
                Func = LovenseDongleOutgoingMessage.MessageFunc.StopSearch,
            });

            /*
            Thread net_rx = new Thread(new ParameterizedThreadStart(DongleCommunicationLoop));
            net_rx.IsBackground = true;
            net_rx.Start();
            */
        }

        bool SendAndExpectSuccess(LovenseDongleOutgoingMessage aMsg)
        {
            var result = SendAndExpectIncomingMessage(aMsg);
            return result.Result >= 200 && result.Result < 300;
        }

        LovenseDongleIncomingMessage SendAndExpectIncomingMessage(LovenseDongleOutgoingMessage aMsg)
        {
            SendMessage(aMsg);
            return ReceiveMessage();
        }

        string SendAndExpectReply(string send)
        {
            string result;
            _sendMute.WaitOne();
            try
            {
                _dongleStream.WriteLine(send);
                result = _dongleStream.ReadLine();
            }
            finally
            {
                _sendMute.ReleaseMutex();
            }


            return result;
        }

        void SendMessage(LovenseDongleOutgoingMessage aMsg)
        {
            if (_dongleStream == null)
            {
                throw new LovenseDongleConnectionException();
            }

            var objStr = JObject.FromObject(aMsg).ToString(Formatting.None);
            // All strings going to the dongle must end with a \r
            objStr += "\r";
            Debug.WriteLine(objStr);
            _sendMute.WaitOne();
            _dongleStream.WriteLine(objStr);
            _sendMute.ReleaseMutex();
        }

        LovenseDongleIncomingMessage ReceiveMessage()
        {
            if (_dongleStream == null)
            {
                throw new LovenseDongleConnectionException();
            }

            var result = _dongleStream.ReadLine();
            var textReader = new StringReader(result);
            // We shouldn't get multiple inputs here, since each object is delimited by a newline.
            var reader = new JsonTextReader(textReader)
            {
                CloseInput = false,
            };

            if (!reader.Read())
            {
                //break;
            }

            var msgObj = JObject.Load(reader);
            return (LovenseDongleIncomingMessage)msgObj.ToObject(typeof(LovenseDongleIncomingMessage), _serializer);
        }

        private void _dongleStream_Closed(object sender, EventArgs e)
        {
            //called when COM port closed. 
            //disconnect all devices and clean up list
            foreach (var dev in _devices)
            {
                dev.Value.Disconnect();
                _devices.Remove(dev.Key);
            }
            //close dongle stream
            _dongleStream = null;
        }

        /*
        private void DongleCommunicationLoop(object obj)
        {
            var result = "";
            var statusRequested = false;
            var dongleScanMode = false;
            var lastToyData = String.Empty;

            while (true)
            {
                // Check that dongle is still connected.
                if (_dongleStream == null)
                {
                    Thread.CurrentThread.Abort();
                }

                if (_runScan && _devices.Count == 0 && dongleScanMode == false && statusRequested == false)
                {
                    //request connected toy list
                    //SendToDongle("{\"type\":\"toy\",\"func\":\"statuss\"}\r");
                    SendToDongle(new LovenseDongleOutgoingMessage()
                    {
                        Type = LovenseDongleOutgoingMessage.MessageType.toy,
                        Func = LovenseDongleOutgoingMessage.MessageFunc.statuss,
                    });
                    statusRequested = true;
                    _dongleStream.ReadTimeout = 500;
                }
                else if (_runScan && _devices.Count == 0 && dongleScanMode == false)
                {
                    //if there no toys connected run search
                    SendToDongle("{\"type\":\"toy\",\"func\":\"search\"}\r");
                    dongleScanMode = true;
                    statusRequested = false;
                    //dongle will keep scanning till something is received, timeout is not much matter
                    _dongleStream.ReadTimeout = 500;
                }
                else if (dongleScanMode == false)
                    _dongleStream.ReadTimeout = 10; //quck read for data coming from dongle, data responces should be handled in send_data

                //check if anything received in tx area
                if (result == "")
                {
                    //for word-reading, need to check it quickly
                    try
                    {
                        result += _dongleStream.ReadLine();
                    }
                    catch
                    {
                    }
                }

                //try parse line
                try
                {
                    if (result != "")
                    {
                        var array = Regex.Replace(result, "[{\" }]", string.Empty).Split(','); //commands array
                        var subarray = array[1].Split(':');

                        switch (subarray[1])
                        {
                            case "status":
                                //incoming toy status message format:
                                //{"type":"toy","func":"status","result":200,"data":{"id":"D188BCDCEBC7","status":202}}
                                var status_match = Regex.Match(result, "((?<={\"id\":\")\\w+).+((?<=\"status\":)\\d+)");
                                if (status_match.Success)
                                {
                                    string id = status_match.Groups[1].Value;
                                    string state = status_match.Groups[2].Value;
                                    if (state == "202")
                                    {
                                        if (dongleScanMode)
                                        {
                                            //cant just connect new toys, stop scanning first
                                            _dongleStream.ReadTimeout = 500;
                                            SendAndWaitFor("{\"type\":\"usb\",\"func\":\"stopSearch\"}\r", "{\"type\":\"usb\",\"func\":\"stopSearch\"", out var res);
                                            dongleScanMode = false;
                                        }
                                        statusRequested = false;
                                        _runScan = false;
                                        //wait a moment before connect
                                        Thread.Sleep(600);
                                        AddToy(id);
                                    }
                                    else
                                    {
                                        //something bad about toy
                                        if (_devices.ContainsKey(id))
                                        {
                                            //delete toy
                                            _devices[id].Disconnect();
                                            _devices.Remove(id);
                                            //toy is gone
                                            while (_sendData.Count > 0)
                                                _sendData.TryDequeue(out var trash);
                                            lastToyData = null;
                                        }

                                    }
                                }
                                break;

                            case "toyData":
                                //extract data message
                                var toyData_match = Regex.Match(result, "((?<={\"id\":\")\\w+).+((?<=\"data\":\")[a-zA-Z0-9;:]+)");
                                if (toyData_match.Success)
                                {
                                    string id = toyData_match.Groups[1].Value;
                                    string data = toyData_match.Groups[2].Value;
                                    if (_devices.ContainsKey(id))
                                    {
                                        _devices[id].ProcessData(data);
                                    }
                                    else
                                    {
                                        //data from unknown toy? it's new!
                                        if (dongleScanMode)
                                        {
                                            //cant just connect new toys, stop scanning first
                                            _dongleStream.ReadTimeout = 500;
                                            SendAndWaitFor("{\"type\":\"usb\",\"func\":\"stopSearch\"}\r", "{\"type\":\"usb\",\"func\":\"stopSearch\"", out var res);
                                        }
                                        dongleScanMode = false;
                                        _runScan = false;
                                        //wait a moment before connect
                                        Thread.Sleep(600);
                                        AddToy(id);
                                    }
                                }
                                break;

                            case "search":
                                {
                                    //get result number
                                    var search_match = Regex.Split(result, "(?<=\"result\":)\\d+");
                                    if (search_match[0] == "205")
                                        dongleScanMode = true;

                                    if (search_match[0] == "206")
                                    {
                                        //dongle search timeout
                                        dongleScanMode = false;
                                        this.InvokeScanningFinished();
                                    }
                                }
                                break;

                            case "stopSearch":
                                if (dongleScanMode)
                                {
                                    dongleScanMode = false;
                                    this.InvokeScanningFinished();
                                }
                                break;

                            case "error":
                                //get result number
                                var error_match = Regex.Split(result, "(?<=\"result\":)\\d+");
                                if (error_match[0] == "501")
                                {
                                    //weird bug when stopSearch sent and ok received but it keeps searching, stop all activity
                                    dongleScanMode = false;
                                }
                                if (error_match[0] == "402")
                                {
                                    //data that was sent to dongle was not accepted
                                    if (_sendData.Count == 0 && lastToyData != null)
                                    {
                                        _sendData.Enqueue(lastToyData);
                                    }
                                }
                                //let dongle output what it got
                                Thread.Sleep(500);
                                break;
                        }
                    }
                }
                catch
                {
                    //Parsing error
                }
                result = "";

                string tosend = "";
                if (dongleScanMode == false && statusRequested == false && _sendData.TryDequeue(out tosend))
                {
                    //command respond rate, it should handle 10hz but sometimes have higher delay (about 103ms, usually ~80ms)
                    _dongleStream.ReadTimeout = 120;
                    lastToyData = tosend; //keep track of last command if it gets lost
                    SendToDongle(tosend);
                    GetFromDongle(out result);
                    if (result == "timeout")
                        result = "";
                }
            }
        }

        async Task AddToy(string id)
        {
            //async creation to avoid locking rx thread
            if (!_devices.ContainsKey(id))
            {
                try
                {
                    var dev = new LovenseDongleDeviceImpl(LogManager, id, _sendData);
                    _devices.Add(id, dev);
                    var bpDevice = await _deviceFactory.CreateDevice(LogManager, dev);
                    InvokeDeviceAdded(new DeviceAddedEventArgs(bpDevice));
                }
                catch (Exception ex)
                {
                    BpLogger.Error($"Cannot connect to Lovense device {id}: {ex.Message}");
                    _devices.Remove(id);
                }
            }
        }

        private void _dongleStream_Closed(object sender, EventArgs e)
        {
            //called when COM port closed. 
            //disconnect all devices and clean up list
            foreach (var dev in _devices)
            {
                dev.Value.Disconnect();
                _devices.Remove(dev.Key);
            }
            //close dongle stream
            _dongleStream = null;
        }

        void SendToDongle(string send)
        {
            if (_dongleStream == null)
                return;
            _sendMute.WaitOne();
            try
            {
                _dongleStream.WriteLine(send);
            }
            catch
            {
            }
            _sendMute.ReleaseMutex();
        }

        void SendToDongle(LovenseDongleOutgoingMessage aMsg)
        {
            if (_dongleStream == null)
            {
                return;
            }

            var objStr = JObject.FromObject(aMsg).ToString(Formatting.None);
            // All strings going to the dongle must end with a \r
            objStr += "\r";
            Debug.WriteLine(objStr);
            _sendMute.WaitOne();
            _dongleStream.WriteLine(objStr);
            _sendMute.ReleaseMutex();
        }

        void GetFromDongle(out string result)
        {
            result = "timeout";
            if (_dongleStream == null)
                return;

            try
            {
                result = _dongleStream.ReadLine();
            }
            catch
            {
            }
        }

        void SendAndExpectReply(string send)
        {
            _sendMute.WaitOne();
            try
            {
                _dongleStream.WriteLine(send);
                result = _dongleStream.ReadLine();
            }
            catch (TimeoutException ex)
            {
            }
            _sendMute.ReleaseMutex();

            return result.StartsWith(wait_start_with);
        }
        */
    }
}
