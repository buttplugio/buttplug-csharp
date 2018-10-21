// <copyright file="IBluetoothDeviceInterface.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth
{
    public interface IBluetoothDeviceInterface
    {
        string Name { get; }

        event EventHandler<BluetoothNotifyEventArgs> BluetoothNotifyReceived;

        event EventHandler DeviceRemoved;

        ulong Address { get; }

        void Disconnect();

        Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<ButtplugMessage> WriteValueAsync(uint aMsgId, uint aCharacteristicIndex, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken);

        Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, uint aCharacteristicIndex, CancellationToken aToken);

        Task SubscribeToUpdatesAsync();

        Task SubscribeToUpdatesAsync(uint aIndex);
    }
}
