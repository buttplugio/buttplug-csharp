// <copyright file="UWPBluetoothDeviceInterface.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
// license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using JetBrains.Annotations;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using Buttplug.Core.Logging;

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
                        c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
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

        public async Task SubscribeToUpdates()
        {
            await SubscribeToUpdates(_rxChar);
        }

        public async Task SubscribeToUpdates(uint aIndex)
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

            await SubscribeToUpdates(_indexedChars[aIndex]);
        }

        private async Task SubscribeToUpdates(GattCharacteristic aCharacteristic)
        {
            if (aCharacteristic == null)
            {
                _bpLogger.Error("Null characteristic passed to SubscribeToUpdates");
                return;
            }

            if (!aCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                _bpLogger.Error($"Cannot subscribe to BLE updates from {Name}: No Notify characteristic found.");
                return;
            }

            var status = await aCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status != GattCommunicationStatus.Success)
            {
                _bpLogger.Error($"Cannot subscribe to BLE updates from {Name}: Failed Request");
                return;
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
            byte[] bytes;
            CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out bytes);
            BluetoothNotifyReceived?.Invoke(this, new BluetoothNotifyEventArgs(bytes));
        }

        public ulong GetAddress()
        {
            return _bleDevice.BluetoothAddress;
        }

        public async Task<ButtplugMessage> WriteValue(uint aMsgId, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            if (_txChar == null)
            {
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"WriteValue using txChar called with no txChar available");
            }

            return await WriteValue(aMsgId, _txChar, aValue, aWriteWithResponse, aToken);
        }

        [ItemNotNull]
        public async Task<ButtplugMessage> WriteValue(uint aMsgId,
            uint aIndex,
            byte[] aValue,
            bool aWriteWithResponse,
            CancellationToken aToken)
        {
            if (_indexedChars == null)
            {
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"WriteValue using indexed characteristics called with no indexed characteristics available");
            }

            if (!_indexedChars.ContainsKey(aIndex))
            {
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"WriteValue using indexed characteristics called with invalid index");
            }

            return await WriteValue(aMsgId, _indexedChars[aIndex], aValue, aWriteWithResponse, aToken);
        }

        private async Task<ButtplugMessage> WriteValue(uint aMsgId,
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
                var status = await writeTask;
                _currentWriteTokenSource = null;
                if (status != GattCommunicationStatus.Success)
                {
                    return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                        $"GattCommunication Error: {status}");
                }
            }
            catch (InvalidOperationException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of
                // a transfer.
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"GattCommunication Error: {e.Message}");
            }
            catch (TaskCanceledException e)
            {
                // This exception will be thrown if the bluetooth device disconnects in the middle of
                // a transfer (happened when MysteryVibe lost power).
                return _bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"Device disconnected: {e.Message}");
            }

            return new Ok(aMsgId);
        }

        public async Task<(ButtplugMessage, byte[])> ReadValue(uint aMsgId, CancellationToken aToken)
        {
            if (_rxChar == null)
            {
                return (_bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"ReadValue using rxChar called with no rxChar available"), new byte[] { });
            }

            return await ReadValue(aMsgId, _rxChar, aToken);
        }

        public async Task<(ButtplugMessage, byte[])> ReadValue(uint aMsgId, uint aIndex, CancellationToken aToken)
        {
            if (_indexedChars == null)
            {
                return (_bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"ReadValue using indexed characteristics called with no indexed characteristics available"), new byte[] { });
            }

            if (!_indexedChars.ContainsKey(aIndex))
            {
                return (_bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE,
                    $"ReadValue using indexed characteristics called with invalid index"), new byte[] { });
            }

            return await ReadValue(aMsgId, _indexedChars[aIndex], aToken);
        }

        private async Task<(ButtplugMessage, byte[])> ReadValue(uint aMsgId, GattCharacteristic aChar, CancellationToken aToken)
        {
            var result = await aChar.ReadValueAsync().AsTask(aToken);
            if (result?.Value == null)
            {
                return (_bpLogger.LogErrorMsg(aMsgId, Error.ErrorClass.ERROR_DEVICE, $"Got null read from {Name}"), null);
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
