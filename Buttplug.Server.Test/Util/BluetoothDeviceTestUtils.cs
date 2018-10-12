// <copyright file="BluetoothDeviceTestUtils.cs" company="Nonpolynomial Labs LLC">
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
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Buttplug.Server.Test.Util
{
    public interface IBluetoothDeviceGeneralTestUtils
    {
        Task SetupTest(string aDeviceName, bool aShouldInitialize = false);

        void TestDeviceInfo();

        Task TestDeviceMessageNoop(ButtplugDeviceMessage aOutgoingMessage);
    }

    public class BluetoothDeviceTestUtils<T> : IBluetoothDeviceGeneralTestUtils
        where T : IBluetoothDeviceInfo, new()
    {
        public readonly uint NoCharacteristic = 0xffffffff;
        private IBluetoothDeviceInfo bleInfo;
        private TestBluetoothDeviceInterface bleIface;
        private IButtplugDevice bleDevice;

        public async Task SetupTest(string aDeviceName, bool aShouldInitialize = true)
        {
            bleInfo = new T();

            // Assume for now that we're using a device's nameprefix as its actual name if we have one.
            Assert.Contains(aDeviceName, bleInfo.Names.Length > 0 ? bleInfo.Names : bleInfo.NamePrefixes);

            bleIface = new TestBluetoothDeviceInterface(aDeviceName);
            bleDevice = bleInfo.CreateDevice(new ButtplugLogManager(), bleIface);

            if (!aShouldInitialize)
            {
                return;
            }

            await Initialize();
        }

        public async Task Initialize()
        {
            Assert.True(await bleDevice.InitializeAsync() is Ok);
            bleIface.LastWritten.Clear();
        }

        public void AddExpectedRead(uint aCharacteristic, byte[] aValue)
        {
            bleIface.AddExpectedRead(aCharacteristic, aValue);
        }

        private void Clear()
        {
            bleIface.LastWritten.Clear();
        }

        public void TestDeviceName(string aExpectedName)
        {
            Assert.AreEqual(aExpectedName, bleDevice.Name);
        }

        public void TestDeviceInfo()
        {
            var info = new T();

            // Device should have at least one service.
            Assert.NotNull(info.Services);
            Assert.True(info.Services.Any());
            Assert.NotNull(info.Services[0]);

            // Device should have a name or nameprefix
            Assert.True(info.Names.Length > 0 || info.NamePrefixes.Length > 0);

            // TODO Device info chrs and characteristics dict should match
        }

        public void TestDeviceAllowedMessages([NotNull] Dictionary<Type, uint> aExpectedMessageArgs)
        {
            Clear();

            // If we've updated a class with new messages, but haven't updated the device, fail.
            Assert.AreEqual(aExpectedMessageArgs.Count, bleDevice.AllowedMessageTypes.Count());
            foreach (var item in aExpectedMessageArgs)
            {
                Assert.True(bleDevice.AllowedMessageTypes.Contains(item.Key));
                Assert.NotNull(bleDevice.GetMessageAttrs(item.Key));
                if (item.Value == 0)
                {
                    Assert.Null(bleDevice.GetMessageAttrs(item.Key).FeatureCount);
                }
                else
                {
                    Assert.AreEqual(item.Value, bleDevice.GetMessageAttrs(item.Key).FeatureCount);
                }
            }
        }

        private void TestPacketMatching(IEnumerable<(byte[], uint)> aExpectedBytes, bool aWriteWithResponse, bool aStrict = true)
        {
            // ExpectedBytes should have the same number of packets as LastWritten
            if (aStrict)
            {
                Assert.AreEqual(aExpectedBytes.Count(), bleIface.LastWritten.Count);

                // Since the expected values and lastwritten in interface should be lockstepped, we can
                // merge them and iterate through everything at once.
                var checkSeq = aExpectedBytes.Zip(bleIface.LastWritten, (first, second) => (first, second));
                foreach (var ((bytes, chr), lastWritten) in checkSeq)
                {
                    Assert.AreEqual(bytes, lastWritten.Value);
                    Assert.AreEqual(aWriteWithResponse, lastWritten.WriteWithResponse);
                    if (chr != NoCharacteristic)
                    {
                        Assert.AreEqual(chr, lastWritten.Characteristic);
                    }
                }
            }
            else
            {
                foreach (var (bytes, chr) in aExpectedBytes)
                {
                    var matched = false;
                    foreach (var lastWritten in bleIface.LastWritten)
                    {
                        if (!lastWritten.Value.SequenceEqual(bytes) ||
                            lastWritten.WriteWithResponse != aWriteWithResponse ||
                            (chr != NoCharacteristic && chr != lastWritten.Characteristic))
                        {
                            continue;
                        }

                        matched = true;
                        break;
                    }

                    var msg = "Matched!";
                    if (!matched)
                    {
                        msg = "Expected:\n";
                        foreach (var (bytes2, chr2) in aExpectedBytes)
                        {
                            var c = chr2 == NoCharacteristic ? "?" : chr2.ToString();
                            msg += $"{BitConverter.ToString(bytes2)} {c} {aWriteWithResponse}\n";
                        }

                        msg += "\nActual:\n";
                        foreach (var lastWritten in bleIface.LastWritten)
                        {
                            msg += $"{BitConverter.ToString(lastWritten.Value)} {lastWritten.Characteristic} {lastWritten.WriteWithResponse}\n";
                        }
                    }

                    Assert.True(matched, $"Expected data must be in send history: {msg}");
                }
            }
        }

        public async Task TestDeviceInitialize(IEnumerable<(byte[], uint)> aExpectedBytes, bool aWriteWithResponse, bool aStrict = true)
        {
            Clear();
            Assert.True(await bleDevice.InitializeAsync() is Ok);

            TestPacketMatching(aExpectedBytes, aWriteWithResponse, aStrict);
        }

        // Testing timing with delays is a great way to get inetermittents, but here we are. Sadness.
        public async Task TestDeviceMessageOnWrite(ButtplugDeviceMessage aOutgoingMessage, IEnumerable<(byte[], uint)> aExpectedBytes, bool aWriteWithResponse)
        {
            Clear();
            Assert.True(await bleDevice.ParseMessageAsync(aOutgoingMessage) is Ok);
            var resetEvent = new ManualResetEvent(false);
            bleIface.ValueWritten += (aObj, aArgs) => { resetEvent.Set(); };
            resetEvent.WaitOne(1000);
            TestPacketMatching(aExpectedBytes, aWriteWithResponse);
        }

        public async Task TestDeviceMessage(ButtplugDeviceMessage aOutgoingMessage, IEnumerable<(byte[], uint)> aExpectedBytes, bool aWriteWithResponse)
        {
            Clear();
            Assert.True(await bleDevice.ParseMessageAsync(aOutgoingMessage) is Ok);

            TestPacketMatching(aExpectedBytes, aWriteWithResponse);
        }

        public async Task TestDeviceMessageDelayed(ButtplugDeviceMessage aOutgoingMessage, IEnumerable<(byte[], uint)> aExpectedBytes, bool aWriteWithResponse, uint aMilliseconds)
        {
            Clear();
            Assert.True(await bleDevice.ParseMessageAsync(aOutgoingMessage) is Ok);
            Thread.Sleep(new TimeSpan(0, 0, 0, 0, (int)aMilliseconds));
            TestPacketMatching(aExpectedBytes, aWriteWithResponse, false);
        }

        public async Task TestDeviceMessageNoop(ButtplugDeviceMessage aOutgoingMessage)
        {
            await TestDeviceMessage(aOutgoingMessage, new List<(byte[], uint)>(), false);
        }

        public async Task TestInvalidDeviceMessage(ButtplugDeviceMessage aOutgoingMessage)
        {
            Clear();
            Assert.True(await bleDevice.ParseMessageAsync(aOutgoingMessage) is Error);
        }

        public async Task TestInvalidVibrateCmd(uint aNumVibes)
        {
            await TestInvalidDeviceMessage(VibrateCmd.Create(4, 1, 0.5, 0));
            await TestInvalidDeviceMessage(VibrateCmd.Create(4, 1, 0.5, aNumVibes + 1));
            await TestInvalidDeviceMessage(
                new VibrateCmd(4, new List<VibrateCmd.VibrateSubcommand>()
                {
                    new VibrateCmd.VibrateSubcommand(0xffffffff, 0.5),
                }));
        }
    }
}