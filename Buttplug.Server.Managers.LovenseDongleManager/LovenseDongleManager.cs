// <copyright file="LovenseDongleManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Devices;
using Buttplug.Devices.Configuration;
using HidSharp;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Buttplug.Server.Managers.LovenseDongleManager
{
    public class LovenseDongleManager : TimedScanDeviceSubtypeManager
    {
        private SerialStream _dongleStream;
        private readonly Mutex _sendMute = new Mutex();
        private readonly SemaphoreSlim _receiveMute = new SemaphoreSlim(0, 1);
        private bool _runScan = false;

        private readonly Dictionary<string, LovenseDongleDeviceImpl> _devices = new Dictionary<string, LovenseDongleDeviceImpl>();
        private ButtplugDeviceFactory _deviceFactory;
        private readonly FixedSizedQueue<string> _sendData = new FixedSizedQueue<string>(2);

        public LovenseDongleManager(IButtplugLogManager aLogManager)
              : base(aLogManager)
        {
        }

        protected override void RunScan()
        {
            // TODO Can the dongle not scan while it's connected to something? Seriously?
            if ((_dongleStream == null && !ScanForDongle()) || _devices.Count > 0)
            {
                return;
            }

            _runScan = true;
            BpLogger.Info("LovenseDongleManager starts scanning");
        }

        void DongleStreamManager(object obj)
        {
            var result = "";
            bool status_requested = false;
            bool dongle_scan_mode = false;
            string last_toy_data = null;

            while (true)
            {
                //check dongle is okay
                if (_dongleStream == null)
                    Thread.CurrentThread.Abort();

                if (_runScan && _devices.Count == 0 && dongle_scan_mode == false && status_requested == false)
                {
                    //request connected toy list
                    SendToDongle("{\"type\":\"toy\",\"func\":\"statuss\"}\r");
                    status_requested = true;
                    _dongleStream.ReadTimeout = 500;
                }
                else if (_runScan && _devices.Count == 0 && dongle_scan_mode == false)
                {
                    //if there no toys connected run search
                    SendToDongle("{\"type\":\"toy\",\"func\":\"search\"}\r");
                    dongle_scan_mode = true;
                    status_requested = false;
                    //dongle will keep scanning till something is received, timeout is not much matter
                    _dongleStream.ReadTimeout = 500;
                }
                else if (dongle_scan_mode == false)
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
                                        if (dongle_scan_mode)
                                        {
                                            //cant just connect new toys, stop scanning first
                                            _dongleStream.ReadTimeout = 500;
                                            SendAndWaitFor("{\"type\":\"usb\",\"func\":\"stopSearch\"}\r", "{\"type\":\"usb\",\"func\":\"stopSearch\"", out var res);
                                            dongle_scan_mode = false;
                                        }
                                        status_requested = false;
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
                                            last_toy_data = null;
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
                                        if (dongle_scan_mode)
                                        {
                                            //cant just connect new toys, stop scanning first
                                            _dongleStream.ReadTimeout = 500;
                                            SendAndWaitFor("{\"type\":\"usb\",\"func\":\"stopSearch\"}\r", "{\"type\":\"usb\",\"func\":\"stopSearch\"", out var res);
                                        }
                                        dongle_scan_mode = false;
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
                                        dongle_scan_mode = true;

                                    if (search_match[0] == "206")
                                    {
                                        //dongle search timeout
                                        dongle_scan_mode = false;
                                        this.InvokeScanningFinished();
                                    }
                                }
                                break;

                            case "stopSearch":
                                if (dongle_scan_mode)
                                {
                                    dongle_scan_mode = false;
                                    this.InvokeScanningFinished();
                                }
                                break;

                            case "error":
                                //get result number
                                var error_match = Regex.Split(result, "(?<=\"result\":)\\d+");
                                if (error_match[0] == "501")
                                {
                                    //weird bug when stopSearch sent and ok received but it keeps searching, stop all activity
                                    dongle_scan_mode = false;
                                }
                                if (error_match[0] == "402")
                                {
                                    //data that was sent to dongle was not accepted
                                    if (_sendData.Count == 0 && last_toy_data != null)
                                    {
                                        _sendData.Enqueue(last_toy_data);
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
                if (dongle_scan_mode == false && status_requested == false && _sendData.TryDequeue(out tosend))
                {
                    //command respond rate, it should handle 10hz but sometimes have higher delay (about 103ms, usually ~80ms)
                    _dongleStream.ReadTimeout = 120;
                    last_toy_data = tosend; //keep track of last command if it gets lost
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

        bool ScanForDongle()
        {
            var devList = DeviceList.Local;
            var serialDevices = devList.GetSerialDevices();
            BpLogger.Info("Scanning for Lovense Dongle");

            foreach (var port in serialDevices)
            {
                // This is the USB device name for the Lovense Dongle
                if (!port.GetFriendlyName().Contains("USB-SERIAL CH340"))
                {
                    continue;
                }

                var config = new OpenConfiguration();
                config.SetOption(OpenOption.Exclusive, true);
                config.SetOption(OpenOption.Interruptible, true);
                config.SetOption(OpenOption.TimeoutIfInterruptible, 1000);
                config.SetOption(OpenOption.TimeoutIfTransient, 1000);

                if (!port.TryOpen(config, out _dongleStream))
                {
                    _dongleStream = null;
                    continue;
                }

                _dongleStream.BaudRate = 115200;
                _dongleStream.DataBits = 8;
                _dongleStream.StopBits = 1;
                _dongleStream.Parity = SerialParity.None;

                if (!_dongleStream.CanRead || !_dongleStream.CanWrite)
                {
                    _dongleStream.Close();
                    _dongleStream = null;
                    continue; //check for sure
                }
                //Dongle respond with only \n, use it as line break. But commands to dongle should be sent as \r\n
                _dongleStream.NewLine = "\n";
                //detect dongle
                _dongleStream.WriteTimeout = 500;
                _dongleStream.ReadTimeout = 500;

                if (!SendAndWaitFor("DeviceType;\r", "D:", out var res))
                {
                    _dongleStream.Close();
                    _dongleStream = null;
                    continue;
                }
                BpLogger.Debug($"Found Lovense Dongle on {port.GetFileSystemName()}");
                _dongleStream.Closed += _dongleStream_Closed;  //(sender, e) => {  };
                //just to make sure no search ongoing
                SendAndWaitFor("{\"type\":\"usb\",\"func\":\"stopSearch\"}\r", "{\"type\":\"usb\",\"func\":\"stopSearch\"", out res);

                Thread net_rx = new Thread(new ParameterizedThreadStart(DongleStreamManager));
                net_rx.IsBackground = true;
                net_rx.Start();

                //you can add more dongles support, but one is enough for now
                return true;
            }
            return false;
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

        bool SendAndWaitFor(string send, string wait_start_with, out string result)
        {
            result = "timeout";

            _sendMute.WaitOne();
            try
            {
                _dongleStream.WriteLine(send);
                result = _dongleStream.ReadLine();
            }
            catch
            {
            }
            _sendMute.ReleaseMutex();

            return result.StartsWith(wait_start_with);
        }

    }
}
