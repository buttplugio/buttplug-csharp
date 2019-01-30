// <copyright file="UWPBluetoothDeviceInterface.cs" company="Nonpolynomial Labs LLC">
//     Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
//     Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
//     license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Devices;
using Buttplug.Devices.Configuration;
using JetBrains.Annotations;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

namespace Buttplug.Server.Managers.UWPBluetoothManager
{
    internal class UWPBluetoothDeviceInterface : ButtplugDeviceImpl
    {
        public override string Name => _bleDevice.Name;

        private readonly Dictionary<string, GattCharacteristic> _indexedChars = new Dictionary<string, GattCharacteristic>();

        [NotNull]
        private CancellationTokenSource _internalTokenSource = new CancellationTokenSource();

        [CanBeNull]
        private CancellationTokenSource _currentWriteTokenSource;

        [CanBeNull]
        private BluetoothLEDevice _bleDevice;

        public override event EventHandler<ButtplugDeviceDataEventArgs> DataReceived;

        public override event EventHandler DeviceRemoved;

        public override string Address => _bleDevice.BluetoothAddress.ToString();

        public override bool Connected => _bleDevice != null && _bleDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;

        protected UWPBluetoothDeviceInterface(
            [NotNull] IButtplugLogManager aLogManager,
            [NotNull] BluetoothLEDevice aDevice)
        : base(aLogManager)
        {
            _bleDevice = aDevice;

            _bleDevice.ConnectionStatusChanged += ConnectionStatusChangedHandler;
        }

        public static async Task<IButtplugDeviceImpl> Create(IButtplugLogManager aLogManager,
            BluetoothLEProtocolConfiguration aConfig,
            BluetoothLEDevice aDevice)
        {
            var device = new UWPBluetoothDeviceInterface(aLogManager, aDevice);
            await device.InitializeDevice(aConfig).ConfigureAwait(false);
            return device;
        }

        protected async Task<GattDeviceService> GetService(Guid aServiceGuid)
        {
            // GetGattServicesForUuidAsync is 15063+ only?
            var serviceResult = await _bleDevice.GetGattServicesForUuidAsync(aServiceGuid, BluetoothCacheMode.Cached);

            // Don't log exceptions here, as we may not want to report them at some points.
            if (serviceResult.Status != GattCommunicationStatus.Success)
            {
                throw new ButtplugDeviceException($"Cannot check for service {aServiceGuid} of {_bleDevice.Name}.");
            }

            if (serviceResult.Services.Count == 0)
            {
                throw new ButtplugDeviceException($"Cannot find service {aServiceGuid} of {_bleDevice.Name}.");
            }

            // TODO is there EVER a way we'd get more than one service back?
            return serviceResult.Services[0];
        }

        protected async Task InitializeDevice(BluetoothLEProtocolConfiguration aConfig)
        {
            // If we don't have any characteristic configuration, assume we're using characteristic detection.
            if (aConfig.Characteristics.Count == 0)
            {
                await AddDefaultCharacteristics(aConfig.Services).ConfigureAwait(false);
            }
            else
            {
                foreach (var characteristicInfo in aConfig.Characteristics)
                {
                    var serviceGuid = characteristicInfo.Key;

                    GattDeviceService service;

                    try
                    {
                        service = await GetService(serviceGuid).ConfigureAwait(false);
                    }
                    catch (ButtplugDeviceException aEx)
                    {
                        // Log, then rethrow.
                        BpLogger.Error(aEx.Message);
                        throw;
                    }

                    var chrResult = await service.GetCharacteristicsAsync();
                    if (chrResult.Status != GattCommunicationStatus.Success)
                    {
                        throw new ButtplugDeviceException(BpLogger,
                            $"Cannot connect to characteristics for {serviceGuid} of {_bleDevice.Name}.");
                    }

                    foreach (var chr in chrResult.Characteristics)
                    {
                        foreach (var indexChr in characteristicInfo.Value)
                        {
                            if (chr.Uuid != indexChr.Value)
                            {
                                continue;
                            }

                            if (_indexedChars.ContainsKey(indexChr.Key))
                            {
                                // We've somehow doubled up endpoint names. Freak out.
                                throw new ButtplugDeviceException(BpLogger, $"Found repeated endpoint name {indexChr.Key} on {Name}");
                            }

                            BpLogger.Debug($"Found characteristic {indexChr.Key} {chr.Uuid} ({_bleDevice.Name})");
                            _indexedChars.Add(indexChr.Key, chr);
                        }
                    }
                }
            }

            if (_indexedChars == null)
            {
                throw new ButtplugDeviceException(BpLogger, $"No characteristics to connect to for device {Name}");
            }
        }

        private async Task AddDefaultCharacteristics(List<Guid> aServices)
        {
            GattDeviceService service = null;
            foreach (var ser in aServices)
            {
                try
                {
                    service = await GetService(ser).ConfigureAwait(false);
                }
                catch (ButtplugDeviceException)
                {
                    // In this case, we may have a whole bunch of services that aren't valid for a
                    // device and only one that is. We can ignore the exception, and throw if we
                    // don't get anything from any service in the list.
                }
            }

            if (service == null)
            {
                throw new ButtplugDeviceException(BpLogger, $"No valid service found for default characteristics of {Name}");
            }

            var chrResult = await service.GetCharacteristicsAsync();
            if (chrResult.Status != GattCommunicationStatus.Success)
            {
                throw new ButtplugDeviceException(BpLogger,
                    $"Cannot connect to characteristics for {service.Uuid} of {_bleDevice.Name}.");
            }

            var chrs = chrResult.Characteristics;

            foreach (var c in chrs)
            {
                if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read) ||
                    c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) ||
                    c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
                {
                    if (_indexedChars.ContainsKey(Endpoints.Rx))
                    {
                        throw new ButtplugDeviceException(BpLogger,
                            $"Too many possible rx characteristics on service {service.Uuid} of {_bleDevice.Name}.");
                    }

                    _indexedChars[Endpoints.Rx] = c;
                }
                else if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse) ||
                         c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                {
                    if (_indexedChars.ContainsKey(Endpoints.Tx))
                    {
                        throw new ButtplugDeviceException(BpLogger,
                            $"Too many possible tx characteristics on service {service.Uuid} of {_bleDevice.Name}.");
                    }

                    _indexedChars[Endpoints.Tx] = c;
                }
            }
        }

        public override async Task SubscribeToUpdatesAsync()
        {
            await SubscribeToUpdatesAsync(_indexedChars[Endpoints.Rx]).ConfigureAwait(false);
        }

        public override async Task SubscribeToUpdatesAsync(string aChrName)
        {
            if (!_indexedChars.ContainsKey(aChrName))
            {
                throw new ButtplugDeviceException(BpLogger, $"SubscribeToUpdates using indexed characteristics called with invalid index {aChrName} on {_bleDevice.Name}.");
            }

            await SubscribeToUpdatesAsync(_indexedChars[aChrName]).ConfigureAwait(false);
        }

        private async Task SubscribeToUpdatesAsync(GattCharacteristic aCharacteristic)
        {
            ButtplugUtils.ArgumentNotNull(aCharacteristic, nameof(aCharacteristic));

            if (!aCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) &&
                !aCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                throw new ButtplugDeviceException(BpLogger, $"Cannot subscribe to BLE updates from {Name}: No Notify characteristic found.");
            }

            if (aCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                var status = await aCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status != GattCommunicationStatus.Success)
                {
                    throw new ButtplugDeviceException(BpLogger, $"Cannot subscribe to BLE notify updates from {Name}: Failed Request {status}");
                }
            }

            if (aCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                var status = await aCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                if (status != GattCommunicationStatus.Success)
                {
                    throw new ButtplugDeviceException(BpLogger, $"Cannot subscribe to BLE indicate updates from {Name}: Failed Request {status}");
                }
            }

            // Server has been informed of clients interest.
            aCharacteristic.ValueChanged += BluetoothNotifyReceivedHandler;
        }

        private void ConnectionStatusChangedHandler([NotNull] BluetoothLEDevice aDevice, [NotNull] object aObj)
        {
            if (_bleDevice?.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                DeviceRemoved?.Invoke(this, new EventArgs());
            }
        }

        private void BluetoothNotifyReceivedHandler(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out var bytes);
            DataReceived?.Invoke(this, new ButtplugDeviceDataEventArgs("rx", bytes));
        }

        public override async Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            return await WriteValueAsync(aMsgId, Endpoints.Tx, aValue, aWriteWithResponse, aToken).ConfigureAwait(false);
        }

        [ItemNotNull]
        public override async Task<ButtplugMessage> WriteValueAsync(uint aMsgId,
            string aChrName,
            byte[] aValue,
            bool aWriteWithResponse,
            CancellationToken aToken)
        {
            if (!_indexedChars.ContainsKey(aChrName))
            {
                throw new ButtplugDeviceException(BpLogger,
                    $"WriteValue using indexed characteristics called with invalid index {aChrName} on {Name}", aMsgId);
            }

            return await WriteValueAsync(aMsgId, _indexedChars[aChrName], aValue, aWriteWithResponse, aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> WriteValueAsync(uint aMsgId,
            GattCharacteristic aChar,
            byte[] aValue,
            bool aWriteWithResponse,
            CancellationToken aToken)
        {
            if (!(_currentWriteTokenSource is null))
            {
                _internalTokenSource.Cancel();
                BpLogger.Error("Cancelling device transfer in progress for new transfer.");
            }

            try
            {
                _internalTokenSource = new CancellationTokenSource();
                _currentWriteTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_internalTokenSource.Token, aToken);
                var writeTask = aChar.WriteValueAsync(aValue.AsBuffer(),
                    aWriteWithResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse).AsTask(_currentWriteTokenSource.Token);
                var status = await writeTask.ConfigureAwait(false);
                _currentWriteTokenSource = null;
                if (status != GattCommunicationStatus.Success)
                {
                    throw new ButtplugDeviceException(BpLogger,
                        $"GattCommunication Error: {status}", aMsgId);
                }
            }
            catch (InvalidOperationException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of
                // a transfer.
                throw new ButtplugDeviceException(BpLogger,
                    $"GattCommunication Error: {e.Message}", aMsgId);
            }
            catch (TaskCanceledException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of
                // a transfer (happened when MysteryVibe lost power).
                throw new ButtplugDeviceException(BpLogger,
                    $"Device disconnected: {e.Message}", aMsgId);
            }

            return new Ok(aMsgId);
        }

        public override async Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken)
        {
            return await ReadValueAsync(aMsgId, Endpoints.Rx, aToken).ConfigureAwait(false);
        }

        public override async Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, string aChrName, CancellationToken aToken)
        {
            if (!_indexedChars.ContainsKey(aChrName))
            {
                throw new ButtplugDeviceException(BpLogger,
                    "ReadValue using indexed characteristics called with invalid index", aMsgId);
            }

            return await ReadValueAsync(aMsgId, _indexedChars[aChrName], aToken).ConfigureAwait(false);
        }

        private async Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, GattCharacteristic aChar, CancellationToken aToken)
        {
            var result = await aChar.ReadValueAsync().AsTask(aToken).ConfigureAwait(false);
            if (result.Status != GattCommunicationStatus.Success)
            {
                throw new ButtplugDeviceException(BpLogger, $"Error while reading from {Name}", aMsgId);
            }

            return (new Ok(aMsgId), result.Value.ToArray());
        }

        public override void Disconnect()
        {
            DeviceRemoved?.Invoke(this, new EventArgs());
            _indexedChars.Clear();

            _bleDevice.Dispose();
            _bleDevice = null;
        }
    }
}