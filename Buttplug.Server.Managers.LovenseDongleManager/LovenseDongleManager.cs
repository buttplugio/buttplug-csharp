/*
 * Lovense Dongle Manager
 *
 *  Created on: 02 April 2019
 *      Author: Vasiliy Sukhoparov (VasiliSk)
 */

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
using Buttplug.Server;
using HidSharp;
using System.Collections.Concurrent;

namespace ButtplugForVR
{
    public class LovenseDongleManager : TimedScanDeviceSubtypeManager
    {
        SerialStream dongle_stream;
        Mutex send_mute = new Mutex();
        SemaphoreSlim receive_mute = new SemaphoreSlim(0, 1);
        bool run_scan = false;

        Dictionary<string, LovenseDongleDeviceImpl> devices; //mabe make single toy only?
        ButtplugDeviceFactory factory;
        FixedSizedQueue<string> send_data = new FixedSizedQueue<string>(2);

        public LovenseDongleManager(IButtplugLogManager aLogManager)
            : base(aLogManager) {

            devices = new Dictionary<string, LovenseDongleDeviceImpl>();
        }

        protected override void RunScan() {
            if (dongle_stream == null && !ScanForDongle())
                return;

            if (devices.Count == 0) {
                run_scan = true;
                BpLogger.Info("LovenseDongleManager starts scanning");
            }
        }

        void DongleStreamManager(object obj) {
            var result = "";
            bool status_requested = false;
            bool dongle_scan_mode = false;
            string last_toy_data = null;

            while (true) {
                //check dongle is okay
                if (dongle_stream == null)
                    Thread.CurrentThread.Abort();

                if (run_scan && devices.Count == 0 && dongle_scan_mode == false && status_requested == false) {
                    //request connected toy list
                    SendToDongle("{\"type\":\"toy\",\"func\":\"statuss\"}\r");
                    status_requested = true;
                    dongle_stream.ReadTimeout = 500;
                }
                else if (run_scan && devices.Count == 0 && dongle_scan_mode == false) {
                    //if there no toys connected run search
                    SendToDongle("{\"type\":\"toy\",\"func\":\"search\"}\r");
                    dongle_scan_mode = true;
                    status_requested = false;
                    //dongle will keep scanning till something is received, timeout is not much matter
                    dongle_stream.ReadTimeout = 500;
                }
                else if (dongle_scan_mode == false)
                    dongle_stream.ReadTimeout = 10; //quck read for data coming from dongle, data responces should be handled in send_data

                //check if anything received in tx area
                if (result == "") {
                    //for word-reading, need to check it quickly
                    try {
                        result += dongle_stream.ReadLine();
                    } catch {
                    }
                }

                //try parse line
                try {
                    if (result != "") {
                        var array = Regex.Replace(result, "[{\" }]", string.Empty).Split(','); //commands array
                        var subarray = array[1].Split(':');

                        switch (subarray[1]) {
                            case "status":
                                //incoming toy status message format:
                                //{"type":"toy","func":"status","result":200,"data":{"id":"D188BCDCEBC7","status":202}}
                                var status_match = Regex.Match(result, "((?<={\"id\":\")\\w+).+((?<=\"status\":)\\d+)");
                                if (status_match.Success) {
                                    string id = status_match.Groups[1].Value;
                                    string state = status_match.Groups[2].Value;
                                    if (state == "202") {
                                        if (dongle_scan_mode) {
                                            //cant just connect new toys, stop scanning first
                                            dongle_stream.ReadTimeout = 500;
                                            SendAndWaitFor("{\"type\":\"usb\",\"func\":\"stopSearch\"}\r", "{\"type\":\"usb\",\"func\":\"stopSearch\"", out var res);
                                            dongle_scan_mode = false;
                                        }
                                        status_requested = false;
                                        run_scan = false;
                                        //wait a moment before connect
                                        Thread.Sleep(600);
                                        AddToy(id);
                                    }
                                    else {
                                        //something bad about toy
                                        if (devices.ContainsKey(id)) {
                                            //delete toy
                                            devices[id].Disconnect();
                                            devices.Remove(id);
                                            //toy is gone
                                            while (send_data.Count > 0)
                                                send_data.TryDequeue(out var trash);
                                            last_toy_data = null;
                                        }

                                    }
                                }
                                break;

                            case "toyData":
                                //extract data message
                                var toyData_match = Regex.Match(result, "((?<={\"id\":\")\\w+).+((?<=\"data\":\")[a-zA-Z0-9;:]+)");
                                if (toyData_match.Success) {
                                    string id = toyData_match.Groups[1].Value;
                                    string data = toyData_match.Groups[2].Value;
                                    if (devices.ContainsKey(id)) {
                                        devices[id].ProcessData(data);
                                    }
                                    else {
                                        //data from unknown toy? it's new!
                                        if (dongle_scan_mode) {
                                            //cant just connect new toys, stop scanning first
                                            dongle_stream.ReadTimeout = 500;
                                            SendAndWaitFor("{\"type\":\"usb\",\"func\":\"stopSearch\"}\r", "{\"type\":\"usb\",\"func\":\"stopSearch\"", out var res);
                                        }
                                        dongle_scan_mode = false;
                                        run_scan = false;
                                        //wait a moment before connect
                                        Thread.Sleep(600);
                                        AddToy(id);
                                    }
                                }
                                break;

                            case "search": {
                                    //get result number
                                    var search_match = Regex.Split(result, "(?<=\"result\":)\\d+");
                                    if (search_match[0] == "205")
                                        dongle_scan_mode = true;

                                    if (search_match[0] == "206") {
                                        //dongle search timeout
                                        dongle_scan_mode = false;
                                        this.InvokeScanningFinished();
                                    }
                                }
                                break;

                            case "stopSearch":
                                if (dongle_scan_mode) {
                                    dongle_scan_mode = false;
                                    this.InvokeScanningFinished();
                                }
                                break;

                            case "error":
                                //get result number
                                var error_match = Regex.Split(result, "(?<=\"result\":)\\d+");
                                if (error_match[0] == "501") {
                                    //weird bug when stopSearch sent and ok received but it keeps searching, stop all activity
                                    dongle_scan_mode = false;
                                }
                                if (error_match[0] == "402") {
                                    //data that was sent to dongle was not accepted
                                    if (send_data.Count == 0 && last_toy_data != null) {
                                        send_data.Enqueue(last_toy_data);
                                    }
                                }
                                //let dongle output what it got
                                Thread.Sleep(500);
                                break;
                        }
                    }
                } catch {
                    //Parsing error
                }
                result = "";

                string tosend = "";
                if (dongle_scan_mode == false && status_requested == false && send_data.TryDequeue(out tosend)) {
                    //command respond rate, it should handle 10hz but sometimes have higher delay (about 103ms, usually ~80ms)
                    dongle_stream.ReadTimeout = 120;
                    last_toy_data = tosend; //keep track of last command if it gets lost
                    SendToDongle(tosend);
                    GetFromDongle(out result);
                    if (result == "timeout")
                        result = "";
                }
            }
        }

        async Task AddToy(string id) {
            //async creation to avoid locking rx thread
            if (!devices.ContainsKey(id)) {
                try {
                    var dev = new LovenseDongleDeviceImpl(LogManager, id, send_data);
                    devices.Add(id, dev);
                    var bpDevice = await factory.CreateDevice(LogManager, dev);
                    InvokeDeviceAdded(new DeviceAddedEventArgs(bpDevice));
                } catch (Exception ex) {
                    BpLogger.Error($"Cannot connect to Lovense device {id}: {ex.Message}");
                    devices.Remove(id);
                }
            }
        }

        bool ScanForDongle() {
            var devList = DeviceList.Local;
            var serialDevices = devList.GetSerialDevices();
            BpLogger.Info("Scanning for Lovense Dongle");

            foreach (var port in serialDevices) {

                var serialFinder = new SerialProtocolConfiguration(port.GetFileSystemName());
                factory = DeviceConfigurationManager.Manager.Find(serialFinder);
                //only lovense dongles
                if (factory == null || factory.ProtocolName != "LovenseProtocol") {
                    continue;
                }

                var config = new OpenConfiguration();
                config.SetOption(OpenOption.Exclusive, true);
                config.SetOption(OpenOption.Interruptible, true);
                config.SetOption(OpenOption.TimeoutIfInterruptible, 1000);
                config.SetOption(OpenOption.TimeoutIfTransient, 1000);

                if (!port.TryOpen(config, out dongle_stream)) {
                    dongle_stream = null;
                    continue;
                }
                var deviceConfig = factory.Config as SerialProtocolConfiguration;

                dongle_stream.BaudRate = (int)deviceConfig.BaudRate;
                dongle_stream.DataBits = (int)deviceConfig.DataBits;
                dongle_stream.StopBits = (int)deviceConfig.StopBits;
                dongle_stream.Parity = SerialParity.None;

                if (!dongle_stream.CanRead || !dongle_stream.CanWrite) {
                    dongle_stream.Close();
                    dongle_stream = null;
                    continue; //check for sure
                }
                //Dongle respond with only \n, use it as line break. But commands to dongle should be sent as \r\n
                dongle_stream.NewLine = "\n";
                //detect dongle
                dongle_stream.WriteTimeout = 500;
                dongle_stream.ReadTimeout = 500;

                if (!SendAndWaitFor("DeviceType;\r", "D:", out var res)) {
                    dongle_stream.Close();
                    dongle_stream = null;
                    continue;
                }
                BpLogger.Debug($"Found Lovense Dongle on {port.GetFileSystemName()}");
                dongle_stream.Closed += Dongle_stream_Closed;  //(sender, e) => {  };
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

        private void Dongle_stream_Closed(object sender, EventArgs e) {
            //called when COM port closed. 
            //disconnect all devices and clean up list
            foreach (var dev in devices) {
                dev.Value.Disconnect();
                devices.Remove(dev.Key);
            }
            //close dongle stream
            dongle_stream = null;
        }

        void SendToDongle(string send) {
            if (dongle_stream == null)
                return;
            send_mute.WaitOne();
            try {
                dongle_stream.WriteLine(send);
            } catch {
            }
            send_mute.ReleaseMutex();
        }

        void GetFromDongle(out string result) {
            result = "timeout";
            if (dongle_stream == null)
                return;

            try {
                result = dongle_stream.ReadLine();
            } catch {
            }
        }

        bool SendAndWaitFor(string send, string wait_start_with, out string result) {
            result = "timeout";

            send_mute.WaitOne();
            try {
                dongle_stream.WriteLine(send);
                result = dongle_stream.ReadLine();
            } catch {
            }
            send_mute.ReleaseMutex();

            return result.StartsWith(wait_start_with);
        }

    }

    public class LovenseDongleDeviceImpl : ButtplugDeviceImpl
    {
        public string DeviceID { get; private set; }
        private bool _connected = true;
        FixedSizedQueue<string> send;

        public LovenseDongleDeviceImpl(IButtplugLogManager aLogManager, string device_id, FixedSizedQueue<string> send_queue)
           : base(aLogManager) {
            Address = device_id;
            DeviceID = device_id;
            send = send_queue;
        }

        public override bool Connected => _connected;

        public override void Disconnect() {
            _connected = false;
            InvokeDeviceRemoved();
        }

        public void ProcessData(string data) {
            this.InvokeDataReceived(new ButtplugDeviceDataEventArgs("rx", Encoding.ASCII.GetBytes(data)));
        }

        public override async Task WriteValueAsyncInternal(byte[] aValue, ButtplugDeviceWriteOptions aOptions, CancellationToken aToken = default(CancellationToken)) {
            var input = Encoding.ASCII.GetString(aValue);
            string format = "{\"type\":\"toy\",\"func\":\"command\",\"id\":\"" + DeviceID + "\",\"cmd\":\"" + input + "\"}\r";
            send.Enqueue(format);
        }

        public override async Task<byte[]> ReadValueAsyncInternal(ButtplugDeviceReadOptions aOptions, CancellationToken aToken = default(CancellationToken)) {
            throw new ButtplugDeviceException("Lovense Dongle Manager: Direct reading not implemented");
        }

        public override Task SubscribeToUpdatesAsyncInternal(ButtplugDeviceReadOptions aOptions) {
            return Task.CompletedTask;
        }
    }

    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private object lockObject = new object();
        public int Limit { get; set; }

        public FixedSizedQueue(int max) : base() {
            Limit = max;
        }

        public new void Enqueue(T obj) {
            base.Enqueue(obj);
            lock (lockObject) {
                while (Count > Limit && base.TryDequeue(out var overflow)) {

                };
            }
        }
    }
}
