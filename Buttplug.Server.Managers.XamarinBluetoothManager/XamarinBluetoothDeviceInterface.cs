// <copyright file="XamarinBlutoothDeviceInterface.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Devices;
using Buttplug.Devices.Configuration;
using JetBrains.Annotations;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buttplug.Server.Managers.XamarinBluetoothManager
{
    public class XamarinBluetoothDeviceInterface : ButtplugDeviceImpl
    {
        [NotNull]
        private readonly Dictionary<string, ICharacteristic> _indexedChars = new Dictionary<string, ICharacteristic>();

        [NotNull]
        private CancellationTokenSource _internalTokenSource = new CancellationTokenSource();

        [CanBeNull]
        private CancellationTokenSource _currentWriteTokenSource;

        [CanBeNull]
        private IDevice _bleDevice;

        public override bool Connected => _bleDevice != null && _bleDevice.State != DeviceState.Disconnected;

        protected XamarinBluetoothDeviceInterface(
            [NotNull] IButtplugLogManager aLogManager,
            [NotNull] IDevice aDevice)
        : base(aLogManager)
        {
            _bleDevice = aDevice;
            Name = _bleDevice.Name;
            Address = _bleDevice.Id.ToString();
        }

        public static async Task<IButtplugDeviceImpl> Create(IButtplugLogManager aLogManager,
            BluetoothLEProtocolConfiguration aConfig,
            IDevice aDevice)
        {
            var device = new XamarinBluetoothDeviceInterface(aLogManager, aDevice);
            await device.InitializeDevice(aConfig).ConfigureAwait(false);
            return device;
        }

        protected async Task<IService> GetService(Guid aServiceGuid)
        {
            var serviceResult = await _bleDevice.GetServiceAsync(aServiceGuid);

            // Don't log exceptions here, as we may not want to report them at some points.
            if (serviceResult == null ||serviceResult.Device.State != DeviceState.Connected)
            {
                throw new ButtplugDeviceException($"Cannot check for service {aServiceGuid} of {_bleDevice.Name}.");
            }

            return serviceResult;
        }

        // Xamarin may sometimes use 16-bit characteristic addressing. Expand to full address so we can use GUIDs.
        protected string CompleteGATTDefaultAddress(string aShortAddr)
        {
            return $"0000{aShortAddr}-0000-1000-8000-00805f9b34fb";
        }

        protected async Task InitializeDevice(BluetoothLEProtocolConfiguration aConfig)
        {
            foreach (var serviceInfo in aConfig.Services)
            {
                // If we don't have any characteristic configuration, assume we're using
                // characteristic detection.
                if (serviceInfo.Value == null || serviceInfo.Value.Count == 0)
                {
                    await AddDefaultCharacteristics(serviceInfo.Key).ConfigureAwait(false);
                }
                else
                {
                    var serviceGuid = serviceInfo.Key;


                    IService service;

                    try
                    {
                        service = await GetService(serviceGuid).ConfigureAwait(false);
                    }
                    catch (ButtplugDeviceException)
                    {
                        // In this case, we may have a whole bunch of services that aren't valid for
                        // a device and only one that is. We can ignore the exception here, and throw
                        // later if we don't get anything from any service in the list.
                        continue;
                    }

                    var chrResult = await service.GetCharacteristicsAsync();

                    if (service.Device.State != DeviceState.Connected)
                    {
                        throw new ButtplugDeviceException(BpLogger,
                            $"Cannot connect to characteristics for {serviceGuid} of {_bleDevice.Name}: {service.Device.State}");
                    }

                    foreach (var chr in chrResult)
                    {
                        var chrUuid = chr.Uuid;
                        if (chrUuid.Length == 4)
                        {
                            chrUuid = CompleteGATTDefaultAddress(chrUuid);
                        }
                        foreach (var indexChr in serviceInfo.Value)
                        {
                            if (Guid.Parse(chrUuid) != indexChr.Value)
                            {
                                continue;
                            }

                            if (_indexedChars.ContainsKey(indexChr.Key))
                            {
                                // We've somehow doubled up endpoint names. Freak out.
                                throw new ButtplugDeviceException(BpLogger, $"Found repeated endpoint name {indexChr.Key} on {Name}");
                            }

                            BpLogger.Debug($"Found characteristic {indexChr.Key} {chrUuid} ({_bleDevice.Name})");
                            _indexedChars.Add(indexChr.Key, chr);
                        }
                    }
                }
            }

            // If we've exited characteristic finding without any characteristics to use, something
            // is wrong with our configuration and we won't be able to talk to the device. Don't
            // continue connection.
            if (!_indexedChars.Any())
            {
                throw new ButtplugDeviceException(BpLogger, $"No characteristics to connect to for device {Name}");
            }
        }

        private async Task AddDefaultCharacteristics(Guid aServiceGuid)
        {
            IService service;
            try
            {
                service = await GetService(aServiceGuid).ConfigureAwait(false);
            }
            catch (ButtplugDeviceException)
            {
                // In this case, we may have a whole bunch of services that aren't valid for a device
                // and only one that is. We can ignore the exception here, and throw later if we
                // don't get anything from any service in the list.
                return;
            }

            // In the case we have multiple services that exist on a device, and no characteristics
            // defined for them, throw, because otherwise we'll end up assigning colliding endpoints.
            if (_indexedChars.ContainsKey(Endpoints.Rx) || _indexedChars.ContainsKey(Endpoints.Tx))
            {
                throw new ButtplugDeviceException(BpLogger,
                    $"Default characteristics already assigned for {Name}.");
            }

            var chrResult = await service.GetCharacteristicsAsync();
            if (service.Device.State != DeviceState.Connected)
            {
                throw new ButtplugDeviceException(BpLogger,
                    $"Cannot connect to characteristics for {service.Id} of {Name}.");
            }

            var chrs = chrResult;

            foreach (var c in chrs)
            {
                if (c.Properties.HasFlag(CharacteristicPropertyType.Read) ||
                    c.Properties.HasFlag(CharacteristicPropertyType.Notify) ||
                    c.Properties.HasFlag(CharacteristicPropertyType.Indicate))
                {
                    if (_indexedChars.ContainsKey(Endpoints.Rx))
                    {
                        throw new ButtplugDeviceException(BpLogger,
                            $"Too many possible rx characteristics on service {service.Id} of {_bleDevice.Name}.");
                    }

                    _indexedChars[Endpoints.Rx] = c;
                }
                else if (c.Properties.HasFlag(CharacteristicPropertyType.WriteWithoutResponse) ||
                         c.Properties.HasFlag(CharacteristicPropertyType.Write))
                {
                    if (_indexedChars.ContainsKey(Endpoints.Tx))
                    {
                        throw new ButtplugDeviceException(BpLogger,
                            $"Too many possible tx characteristics on service {service.Id} of {_bleDevice.Name}.");
                    }

                    _indexedChars[Endpoints.Tx] = c;
                }
            }
        }

        public override async Task SubscribeToUpdatesAsyncInternal(ButtplugDeviceReadOptions aOptions)
        {
            if (!_indexedChars.ContainsKey(aOptions.Endpoint))
            {
                throw new ButtplugDeviceException(BpLogger, $"Endpoint {aOptions.Endpoint} does not exist on device {Name}.");
            }
            await SubscribeToUpdatesAsync(_indexedChars[aOptions.Endpoint]).ConfigureAwait(false);
        }


        private async Task SubscribeToUpdatesAsync(ICharacteristic aCharacteristic)
        {
            aCharacteristic.ValueUpdated += BluetoothNotifyReceivedHandler;
            await aCharacteristic.StartUpdatesAsync();
        }

        private void BluetoothNotifyReceivedHandler(object sender, CharacteristicUpdatedEventArgs e)
        {
            InvokeDataReceived(new ButtplugDeviceDataEventArgs("rx", e.Characteristic.Value));
        }

        [ItemNotNull]
        public override async Task WriteValueAsyncInternal(byte[] aValue,
            ButtplugDeviceWriteOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            if (!_indexedChars.ContainsKey(aOptions.Endpoint))
            {
                throw new ButtplugDeviceException(BpLogger,
                    $"WriteValue using indexed characteristics called with invalid index {aOptions.Endpoint} on {Name}");
            }

            await WriteValueAsync(_indexedChars[aOptions.Endpoint], aValue, aOptions.WriteWithResponse, aToken).ConfigureAwait(false);
        }

        private async Task WriteValueAsync(ICharacteristic aChar,
            byte[] aValue,
            bool aWriteWithResponse,
            CancellationToken aToken)
        {
            if (!(_currentWriteTokenSource is null))
            {
                _internalTokenSource.Cancel();
                BpLogger.Error("Cancelling device transfer in progress for new transfer.");
            }

            if (_bleDevice.State != DeviceState.Connected)
            {
                throw new ButtplugDeviceException(BpLogger,
                    $"Device {Name} not connected.");
            }

            try
            {
                _internalTokenSource = new CancellationTokenSource();
                _currentWriteTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_internalTokenSource.Token, aToken);
                var writeTask = aChar.WriteAsync(aValue,_currentWriteTokenSource.Token);
                var status = await writeTask.ConfigureAwait(false);
                _currentWriteTokenSource = null;
            }
            catch (InvalidOperationException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of
                // a transfer.
                throw new ButtplugDeviceException(BpLogger,
                    $"GattCommunication Error: {e.Message}");
            }
            catch (TaskCanceledException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of
                // a transfer (happened when MysteryVibe lost power).
                throw new ButtplugDeviceException(BpLogger,
                    $"Device disconnected: {e.Message}");
            }
        }

        public override async Task<byte[]> ReadValueAsyncInternal(ButtplugDeviceReadOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            if (!_indexedChars.ContainsKey(aOptions.Endpoint))
            {
                throw new ButtplugDeviceException(BpLogger,
                    "ReadValue using indexed characteristics called with invalid index");
            }

            return await ReadValueAsync(_indexedChars[aOptions.Endpoint], aToken).ConfigureAwait(false);
        }

        private async Task<byte[]> ReadValueAsync(ICharacteristic aChar, CancellationToken aToken)
        {
            if (_bleDevice.State != DeviceState.Connected)
            {
                throw new ButtplugDeviceException(BpLogger, $"Device {Name} not connected.");
            }

            return await aChar.ReadAsync(aToken).ConfigureAwait(false);
        }

        public override void Disconnect()
        {
            InvokeDeviceRemoved();
            _indexedChars.Clear();

            _bleDevice = null;
        }
    }
}
