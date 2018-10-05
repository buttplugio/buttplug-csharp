// <copyright file="TestBluetoothDeviceInterface.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    public class TestBluetoothDeviceInterface : IBluetoothDeviceInterface
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx = 1,
            Extra = 2,
        }

        public string Name { get; }

        private readonly ulong _address;

        public class WriteData
        {
            public uint MsgId;
            public uint Characteristic;
            public byte[] Value;
            public bool WriteWithResponse;
        }

        public class ReadData
        {
            public uint Characteristic;
            public byte[] Value;
        }

        public ulong Address => _address;

        public List<WriteData> LastWritten = new List<WriteData>();
        public Dictionary<uint, List<byte[]>> ExpectedRead = new Dictionary<uint, List<byte[]>>();

        public event EventHandler DeviceRemoved;

        public event EventHandler ValueWritten;

#pragma warning disable CS0067 // Unused event (We'll use it once we have more notifications)
        public event EventHandler<BluetoothNotifyEventArgs> BluetoothNotifyReceived;
#pragma warning restore CS0067

        public bool Removed;

        public TestBluetoothDeviceInterface(string aName)
        {
            Name = aName;
            _address = (ulong)new Random().Next(0, 100);
            Removed = false;
            DeviceRemoved += (obj, args) => { Removed = true; };
        }

        public void AddExpectedRead(uint aCharacteristicIndex, byte[] aValue)
        {
            if (!ExpectedRead.ContainsKey(aCharacteristicIndex))
            {
                ExpectedRead.Add(aCharacteristicIndex, new List<byte[]>());
            }

            ExpectedRead[aCharacteristicIndex].Add(aValue);
        }

        public Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            return WriteValueAsync(aMsgId, (uint)Chrs.Tx, aValue, aWriteWithResponse, aToken);
        }

        public Task<ButtplugMessage> WriteValueAsync(uint aMsgId, uint aCharacteristic, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            LastWritten.Add(new WriteData()
            {
                Value = (byte[])aValue.Clone(),
                MsgId = aMsgId,
                Characteristic = aCharacteristic,
                WriteWithResponse = aWriteWithResponse,
            });
            ValueWritten?.Invoke(this, new EventArgs());
            return Task.FromResult<ButtplugMessage>(new Ok(aMsgId));
        }

        public Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken)
        {
            // Expect that we'll only have one entry in the dictionary at this point.
            Assert.AreEqual(ExpectedRead.Count, 1);
            var value = ExpectedRead[ExpectedRead.Keys.ToArray()[0]].ElementAt(0);
            ExpectedRead[ExpectedRead.Keys.ToArray()[0]].RemoveAt(0);
            return Task.FromResult<(ButtplugMessage, byte[])>((new Ok(aMsgId), value));
        }

        public Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, uint aIndex, CancellationToken aToken)
        {
            return Task.FromResult<(ButtplugMessage, byte[])>((new Ok(aMsgId), new byte[] { }));
        }

        // noop for tests
        public Task SubscribeToUpdatesAsync()
        {
            return Task.CompletedTask;
        }

        // noop for tests
        public Task SubscribeToUpdatesAsync(uint aIndex)
        {
            return Task.CompletedTask;
        }

        public void Disconnect()
        {
            DeviceRemoved?.Invoke(this, new EventArgs());
        }
    }
}