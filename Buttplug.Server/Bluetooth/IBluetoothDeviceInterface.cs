// <copyright file="IBluetoothDeviceInterface.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;

namespace Buttplug.Server.Bluetooth
{
    public interface IBluetoothDeviceInterface
    {
        string Name { get; }

        event EventHandler<BluetoothNotifyEventArgs> BluetoothNotifyReceived;

        Task<ButtplugMessage> WriteValue(uint aMsgId, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<ButtplugMessage> WriteValue(uint aMsgId, uint aCharactieristicIndex, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        // TODO If Unity requires < 4.7, this may need to be changed to use out params instead of tuple returns.
        Task<(ButtplugMessage, byte[])> ReadValue(uint aMsgId, CancellationToken aToken);

        // TODO If Unity requires < 4.7, this may need to be changed to use out params instead of tuple returns.
        Task<(ButtplugMessage, byte[])> ReadValue(uint aMsgId, uint aCharacteristicIndex, CancellationToken aToken);

        Task SubscribeToUpdates();

        Task SubscribeToUpdates(uint aIndex);

        ulong GetAddress();

        event EventHandler DeviceRemoved;

        void Disconnect();
    }
}
