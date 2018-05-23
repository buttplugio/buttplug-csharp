// <copyright file="UWPBluetoothDeviceInterface.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
// license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using JetBrains.Annotations;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

namespace Buttplug.Server.Managers.UWPBluetoothManager
{
    internal class UWPBluetoothDeviceInterface : IBluetoothDeviceInterface
    {
        public string Name => _bleDevice.Name;

        private readonly Dictionary<uint, GattCharacteristic> _indexedChars;

        [NotNull]
        private readonly IButtplugLog _bpLogger;

        private GattCharacteristic _txChar;

        private GattCharacteristic _rxChar;

        [NotNull]
        private CancellationTokenSource _internalTokenSource = new CancellationTokenSource();

        [CanBeNull]
        private CancellationTokenSource _currentWriteTokenSource;

        [CanBeNull]
        private BluetoothLEDevice _bleDevice;

        [CanBeNull]
        public event EventHandler DeviceRemoved;

        public ulong Address => _bleDevice.BluetoothAddress;

        [CanBeNull]
        public event EventHandler<BluetoothNotifyEventArgs> BluetoothNotifyReceived;

        public UWPBluetoothDeviceInterface(
            [NotNull] IButtplugLogManager aLogManager,
            [NotNull] IBluetoothDeviceInfo aInfo,
            [NotNull] BluetoothLEDevice aDevice,
            [NotNull] GattCharacteristic[] aChars)
        {
            _bpLogger = aLogManager.GetLogger(GetType());
            _bleDevice = aDevice;

            if (aInfo.Characteristics.Count > 0)
            {
                foreach (var item in aInfo.Characteristics)
                {
                    var c = (from x in aChars
                             where x.Uuid == item.Value
                             select x).ToArray();
                    if (c.Length != 1)
                    {
                        var err = $"Cannot find characteristic ${item.Value} for device {Name}";
                        _bpLogger.Error(err);
                        throw new Exception(err);
                    }

                    if (_indexedChars == null)
                    {
                        _indexedChars = new Dictionary<uint, GattCharacteristic>();
                    }

                    _indexedChars.Add(item.Key, c[0]);
                }
            }
            else
            {
                foreach (var c in aChars)
                {
                    if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read) ||
                        c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) ||
                        c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
                    {
                        _rxChar = c;
                    }
                    else if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse) ||
                             c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                    {
                        _txChar = c;
                    }
                }
            }

            if (_rxChar == null && _txChar == null && _indexedChars == null)
            {
                var err = $"No characteristics to connect to for device {Name}";
                _bpLogger.Error(err);
                throw new Exception(err);
            }

            _bleDevice.ConnectionStatusChanged += ConnectionStatusChangedHandler;
        }

        public async Task SubscribeToUpdatesAsync()
        {
            await SubscribeToUpdatesAsync(_rxChar).ConfigureAwait(false);
        }

        public async Task SubscribeToUpdatesAsync(uint aIndex)
        {
            if (_indexedChars == null)
            {
                _bpLogger.Error("SubscribeToUpdates using indexed characteristics called with no indexed characteristics available");
                return;
            }

            if (!_indexedChars.ContainsKey(aIndex))
            {
                _bpLogger.Error("SubscribeToUpdates using indexed characteristics called with invalid index");
                return;
            }

            await SubscribeToUpdatesAsync(_indexedChars[aIndex]).ConfigureAwait(false);
        }

        private async Task SubscribeToUpdatesAsync(GattCharacteristic aCharacteristic)
        {
            if (aCharacteristic == null)
            {
                _bpLogger.Error("Null characteristic passed to SubscribeToUpdates");
                return;
            }

            if (!aCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify) &&
                !aCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                _bpLogger.Error($"Cannot subscribe to BLE updates from {Name}: No Notify characteristic found.");
                return;
            }

            if (aCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                var status = await aCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status != GattCommunicationStatus.Success)
                {
                    _bpLogger.Error($"Cannot subscribe to BLE notify updates from {Name}: Failed Request {status}");
                }
            }

            if (aCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                var status = await aCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                if (status != GattCommunicationStatus.Success)
                {
                    _bpLogger.Error($"Cannot subscribe to BLE indicate updates from {Name}: Failed Request {status}");
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
            BluetoothNotifyReceived?.Invoke(this, new BluetoothNotifyEventArgs(bytes));
        }

        public async Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            if (_txChar == null)
            {
                throw new ButtplugDeviceException(_bpLogger, "WriteValue using txChar called with no txChar available", aMsgId);
            }

            return await WriteValueAsync(aMsgId, _txChar, aValue, aWriteWithResponse, aToken).ConfigureAwait(false);
        }

        [ItemNotNull]
        public async Task<ButtplugMessage> WriteValueAsync(uint aMsgId,
            uint aIndex,
            byte[] aValue,
            bool aWriteWithResponse,
            CancellationToken aToken)
        {
            if (_indexedChars == null)
            {
                throw new ButtplugDeviceException(_bpLogger, "WriteValue using indexed characteristics called with no indexed characteristics available", aMsgId);
            }

            if (!_indexedChars.ContainsKey(aIndex))
            {
                throw new ButtplugDeviceException(_bpLogger,
                    "WriteValue using indexed characteristics called with invalid index", aMsgId);
            }

            return await WriteValueAsync(aMsgId, _indexedChars[aIndex], aValue, aWriteWithResponse, aToken).ConfigureAwait(false);
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
                _bpLogger.Error("Cancelling device transfer in progress for new transfer.");
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
                    throw new ButtplugDeviceException(_bpLogger,
                        $"GattCommunication Error: {status}", aMsgId);
                }
            }
            catch (InvalidOperationException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of
                // a transfer.
                throw new ButtplugDeviceException(_bpLogger,
                    $"GattCommunication Error: {e.Message}", aMsgId);
            }
            catch (TaskCanceledException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of
                // a transfer (happened when MysteryVibe lost power).
                throw new ButtplugDeviceException(_bpLogger,
                    $"Device disconnected: {e.Message}", aMsgId);
            }

            return new Ok(aMsgId);
        }

        public async Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken)
        {
            if (_rxChar == null)
            {
                throw new ButtplugDeviceException(_bpLogger,
                    "ReadValue using rxChar called with no rxChar available", aMsgId);
            }

            return await ReadValueAsync(aMsgId, _rxChar, aToken).ConfigureAwait(false);
        }

        public async Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, uint aIndex, CancellationToken aToken)
        {
            if (_indexedChars == null)
            {
                throw new ButtplugDeviceException(_bpLogger,
                    "ReadValue using indexed characteristics called with no indexed characteristics available", aMsgId);
            }

            if (!_indexedChars.ContainsKey(aIndex))
            {
                throw new ButtplugDeviceException(_bpLogger,
                    "ReadValue using indexed characteristics called with invalid index", aMsgId);
            }

            return await ReadValueAsync(aMsgId, _indexedChars[aIndex], aToken).ConfigureAwait(false);
        }

        private async Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, GattCharacteristic aChar, CancellationToken aToken)
        {
            var result = await aChar.ReadValueAsync().AsTask(aToken).ConfigureAwait(false);
            if (result.Status != GattCommunicationStatus.Success)
            {
                throw new ButtplugDeviceException(_bpLogger, $"Error while reading from {Name}", aMsgId);
            }

            return (new Ok(aMsgId), result.Value.ToArray());
        }

        public void Disconnect()
        {
            DeviceRemoved?.Invoke(this, new EventArgs());
            _txChar = null;
            _rxChar = null;
            _indexedChars.Clear();

            _bleDevice.Dispose();
            _bleDevice = null;
        }
    }
}
