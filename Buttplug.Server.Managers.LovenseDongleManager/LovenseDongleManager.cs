// <copyright file="LovenseDongleManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    {
    }

    public class LovenseDongleManager : DeviceSubtypeManager
    {
        private SerialStream _dongleStream;
        private bool _runScan = false;

        private readonly Dictionary<string, LovenseDongleDeviceImpl> _devices =
            new Dictionary<string, LovenseDongleDeviceImpl>();

        private ConcurrentQueue<Tuple<string, TaskCompletionSource<LovenseDongleIncomingMessage>>> CommQueue = 
            new ConcurrentQueue<Tuple<string, TaskCompletionSource<LovenseDongleIncomingMessage>>>();
        private ButtplugDeviceFactory _deviceFactory;
        private readonly FixedSizedQueue<LovenseDongleOutgoingMessage> _msgQueue = new FixedSizedQueue<LovenseDongleOutgoingMessage>(2);
        private readonly JsonSerializer _serializer = new JsonSerializer();
        private Task _dongleCommTask;
        private Task _scanForDongleTask;
        private bool _isScanning;
        private CancellationTokenSource _portReadTokenSource = new CancellationTokenSource();

        public LovenseDongleManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            _scanForDongleTask = new Task(async () =>
            {
                var foundDongle = false;
                while (_dongleStream == null)
                {
                    // TODO This should probably watch for changes in the connected devices list somehow, instead of running a loop.
                    try
                    {
                        await ScanForDongle();
                    }
                    catch (LovenseDongleConnectionException ex)
                    {
                        // Connection didn't work this time. Sleep for a bit and try finding it again.
                        await Task.Delay(2000);
                    }
                }
            });
            _scanForDongleTask.Start();
        }

        private async Task<bool> MatchToWaitingTasks(LovenseDongleIncomingMessage aMsg)
        {
            // command matches to toyData

            // search matches to search

            // stopSearch matches to stopSearch
            return false;
        }

        private async Task DongleCommunicationLoop()
        {
            while (true)
            {
                // Make a new token for the next loop.
                _portReadTokenSource = new CancellationTokenSource();

                // Wait for incoming messages, until we cancel out.
                var result = await ReceiveMessageAsync(_portReadTokenSource.Token);

                // IF we have a message to check, see whether we're waiting for this message type.
                if (result != null && !await MatchToWaitingTasks(result))
                {
                    // If not, then run through incoming message scenarios.
                    switch (result.Func)
                    {
                        case LovenseDongleMessageFunc.ToyData:
                            if (!_devices.ContainsKey(result.Data.Id))
                            {
                                // We've found a new toy.
                                await AddToy(result.Id);
                            }

                            break;
                        case LovenseDongleMessageFunc.IncomingStatus:
                            if (result.Data != null)
                            {
                                if (result.Data.Status == (uint) LovenseDongleResultCode.DeviceDisconnected)
                                {
                                    // Device disconnected

                                    // TODO Should we try to enter a reconnection loop here?
                                }
                            }

                            break;
                        case LovenseDongleMessageFunc.Search:
                            // When calling stopSearch, Search will return with a 206 to denote that searching has stopped.
                            break;
                        default:
                            break;
                    }
                }

                // Send out anything we have waiting.
                while (_msgQueue.Any())
                {
                    _msgQueue.TryDequeue(out var msg);
                    await SendMessageAsync(msg);
                }
            }
        }

        private async Task SendMessageAsync(LovenseDongleOutgoingMessage aMsg)
        {
            if (_dongleStream == null)
            {
                throw new LovenseDongleConnectionException();
            }

            var objStr = JObject.FromObject(aMsg).ToString(Formatting.None);
            // All strings going to the dongle must end with a \r
            objStr += "\r";
            await WriteLineAsync(objStr);
        }

        private async Task<LovenseDongleIncomingMessage> ReceiveMessageAsync(CancellationToken aToken)
        {
            if (_dongleStream == null)
            {
                throw new LovenseDongleConnectionException();
            }

            var result = await ReadLineAsync();
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


        private async Task ScanForDongle()
        {
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

            if (devPort == null)
            {
                throw new LovenseDongleConnectionException();
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
                throw new LovenseDongleConnectionException();
            }

            _dongleStream.BaudRate = 115200;
            _dongleStream.DataBits = 8;
            _dongleStream.StopBits = 1;
            _dongleStream.Parity = SerialParity.None;

            if (!_dongleStream.CanRead || !_dongleStream.CanWrite)
            {
                _dongleStream.Close();
                _dongleStream = null;
                throw new LovenseDongleConnectionException();
            }

            // Dongle responds with only \n, use it as line break. But commands to dongle should be sent as \r\n
            _dongleStream.NewLine = "\n";

            // Detect dongle
            _dongleStream.WriteTimeout = 500;
            _dongleStream.ReadTimeout = 500;

            try
            {
                // Send a Lovense style device type query, which will return a "D" toy code.
                var reply = await SendAndExpectReply("DeviceType;\r");
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
                throw new LovenseDongleConnectionException();
            }

            _dongleStream.Closed += _dongleStream_Closed;

            var result = await SendAndExpectSuccess(new LovenseDongleOutgoingMessage()
            {
                Type = LovenseDongleMessageType.USB,
                Func = LovenseDongleMessageFunc.StopSearch,
            });
        }

        private async Task<bool> SendAndExpectSuccess(LovenseDongleOutgoingMessage aMsg)
        {
            var result = await SendAndExpectIncomingMessage(aMsg);
            return result.Result >= 200 && result.Result < 300;
        }

        private async Task<LovenseDongleIncomingMessage> SendAndExpectIncomingMessage(LovenseDongleOutgoingMessage aMsg)
        {
            var resultSource = new TaskCompletionSource<LovenseDongleIncomingMessage>();

            await resultSource.Task;
            await SendMessageAsync(aMsg);
            return await ReceiveMessageAsync();
        }

        private async Task WriteLineAsync(string aLine)
        {
            var writeTask = new Task(() => _dongleStream.WriteLine(aLine));
            writeTask.Start();
            await writeTask;
        }

        private async Task<string> ReadLineAsync(CancellationToken aToken)
        {
            var readTask = new Task<string>(() => _dongleStream.ReadLine(), aToken);
            readTask.Start();
            return await readTask;
        }

        private async Task<string> SendAndExpectReply(string aMsg)
        {
            string result;

            await WriteLineAsync(aMsg);
            result = await ReadLineAsync();

            return result;
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
        
        public override async Task StartScanning()
        {
            if (_dongleStream == null)
            {
                throw new LovenseDongleConnectionException();
            }

            if (_dongleCommTask == null || _dongleCommTask.IsCompleted)
            {
                _dongleCommTask = new Task(() => DongleCommunicationLoop());
                _dongleCommTask.Start();
            }

            await SendAndExpectSuccess(new LovenseDongleOutgoingMessage()
            {
                Type = LovenseDongleMessageType.USB,
                Func = LovenseDongleMessageFunc.Search,
            });

            _isScanning = true;
        }

        public override async Task StopScanning()
        {
            if (_dongleStream == null)
            {
                throw new LovenseDongleConnectionException();
            }

            await SendAndExpectSuccess(new LovenseDongleOutgoingMessage()
            {
                Type = LovenseDongleMessageType.USB,
                Func = LovenseDongleMessageFunc.StopSearch,
            });
            _isScanning = false;
        }

        public override bool IsScanning()
        {
            return _isScanning;
        }

        private async Task AddToy(string aDeviceId)
        {
            //async creation to avoid locking rx thread
            if (_devices.ContainsKey(aDeviceId))
            {
                return;
            }

            try
            {
                var dev = new LovenseDongleDeviceImpl(LogManager, aDeviceId, _msgQueue);
                _devices.Add(aDeviceId, dev);
                var bpDevice = await _deviceFactory.CreateDevice(LogManager, dev);
                InvokeDeviceAdded(new DeviceAddedEventArgs(bpDevice));
            }
            catch (Exception ex)
            {
                BpLogger.Error($"Cannot connect to Lovense dongle device {aDeviceId}: {ex.Message}");
                _devices.Remove(aDeviceId);
            }
        }


        /*
        private async Task DongleCommunicationLoop()
        {
            var result = "";
            var statusRequested = false;
            var dongleScanMode = false;
            var lastToyData = string.Empty;
    
            while (true)
            {
                // Check that dongle is still connected.
                if (_dongleStream == null)
                {
                    return;
                }
    
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
        /*
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