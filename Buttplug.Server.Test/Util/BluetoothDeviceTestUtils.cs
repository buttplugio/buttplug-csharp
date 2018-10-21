// <copyright file="BluetoothDeviceTestUtils.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Buttplug.Server.Test.Util
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    public interface IBluetoothDeviceGeneralTestUtils
    {
        Task SetupTest(string aDeviceName, bool aShouldInitialize = false);

        void TestDeviceInfo();

        Task TestDeviceMessageNoop(ButtplugDeviceMessage aOutgoingMessage);
    }

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
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
            (bleInfo.Names.Length > 0 ? bleInfo.Names : bleInfo.NamePrefixes).Should().Contain(aDeviceName);

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
            (await bleDevice.InitializeAsync()).Should().BeOfType<Ok>();
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
            bleDevice.Name.Should().Be(aExpectedName);
        }

        public void TestDeviceInfo()
        {
            var info = new T();

            // Device should have at least one service.
            info.Services.Should().NotBeNull();
            info.Services.Any().Should().BeTrue();

            // Device should have a name or nameprefix
            (info.Names.Length > 0 || info.NamePrefixes.Length > 0).Should().BeTrue();

            // TODO Device info chrs and characteristics dict should match
        }

        public void TestDeviceAllowedMessages([NotNull] Dictionary<Type, uint> aExpectedMessageArgs)
        {
            Clear();

            // If we've updated a class with new messages, but haven't updated the device, fail.
            bleDevice.AllowedMessageTypes.Count().Should().Be(aExpectedMessageArgs.Count);
            foreach (var item in aExpectedMessageArgs)
            {
                bleDevice.AllowedMessageTypes.Should().Contain(item.Key);
                bleDevice.GetMessageAttrs(item.Key).Should().NotBeNull();
                if (item.Value == 0)
                {
                    bleDevice.GetMessageAttrs(item.Key).FeatureCount.Should().BeNull();
                }
                else
                {
                    bleDevice.GetMessageAttrs(item.Key).FeatureCount.Should().Be(item.Value);
                }
            }
        }

        private void TestPacketMatching(IEnumerable<(byte[], uint)> aExpectedBytes, bool aWriteWithResponse, bool aStrict = true)
        {
            // ExpectedBytes should have the same number of packets as LastWritten
            if (aStrict)
            {
                bleIface.LastWritten.Count.Should().Be(aExpectedBytes.Count());

                // Since the expected values and lastWritten in interface should be lock-stepped, we can
                // merge them and iterate through everything at once.
                var checkSeq = aExpectedBytes.Zip(bleIface.LastWritten, (first, second) => (first, second));
                foreach (var ((bytes, chr), lastWritten) in checkSeq)
                {
                    lastWritten.Value.Should().Equal(bytes);
                    lastWritten.WriteWithResponse.Should().Be(aWriteWithResponse);
                    if (chr != NoCharacteristic)
                    {
                        lastWritten.Characteristic.Should().Be(chr);
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

                    matched.Should().BeTrue();
                }
            }
        }

        public async Task TestDeviceInitialize(IEnumerable<(byte[], uint)> aExpectedBytes, bool aWriteWithResponse, bool aStrict = true)
        {
            Clear();
            (await bleDevice.InitializeAsync()).Should().BeOfType<Ok>();

            TestPacketMatching(aExpectedBytes, aWriteWithResponse, aStrict);
        }

        // Testing timing with delays is a great way to get intermittent errors, but here we are. Sadness.
        public async Task TestDeviceMessageOnWrite(ButtplugDeviceMessage aOutgoingMessage, IEnumerable<(byte[], uint)> aExpectedBytes, bool aWriteWithResponse)
        {
            Clear();
            (await bleDevice.ParseMessageAsync(aOutgoingMessage)).Should().BeOfType<Ok>();
            var resetEvent = new ManualResetEvent(false);
            bleIface.ValueWritten += (aObj, aArgs) => { resetEvent.Set(); };
            resetEvent.WaitOne(1000);
            TestPacketMatching(aExpectedBytes, aWriteWithResponse);
        }

        public async Task TestDeviceMessage(ButtplugDeviceMessage aOutgoingMessage, IEnumerable<(byte[], uint)> aExpectedBytes, bool aWriteWithResponse)
        {
            Clear();
            (await bleDevice.ParseMessageAsync(aOutgoingMessage)).Should().BeOfType<Ok>();

            TestPacketMatching(aExpectedBytes, aWriteWithResponse);
        }

        public async Task TestDeviceMessageDelayed(ButtplugDeviceMessage aOutgoingMessage, IEnumerable<(byte[], uint)> aExpectedBytes, bool aWriteWithResponse, uint aMilliseconds)
        {
            Clear();
            (await bleDevice.ParseMessageAsync(aOutgoingMessage)).Should().BeOfType<Ok>();
            Thread.Sleep(new TimeSpan(0, 0, 0, 0, (int)aMilliseconds));
            TestPacketMatching(aExpectedBytes, aWriteWithResponse, false);
        }

        public async Task TestDeviceMessageNoop(ButtplugDeviceMessage aOutgoingMessage)
        {
            await TestDeviceMessage(aOutgoingMessage, new List<(byte[], uint)>(), false);
        }

        public void TestInvalidDeviceMessage(ButtplugDeviceMessage aOutgoingMessage)
        {
            Clear();
            bleDevice.Awaiting(async aDev => await aDev.ParseMessageAsync(aOutgoingMessage)).Should().Throw<ButtplugDeviceException>();
        }

        public void TestInvalidVibrateCmd(uint aNumVibes)
        {
            TestInvalidDeviceMessage(VibrateCmd.Create(4, 1, 0.5, 0));
            TestInvalidDeviceMessage(VibrateCmd.Create(4, 1, 0.5, aNumVibes + 1));
            TestInvalidDeviceMessage(
                new VibrateCmd(4, new List<VibrateCmd.VibrateSubcommand>()
                {
                    new VibrateCmd.VibrateSubcommand(0xffffffff, 0.5),
                }));
        }
    }
}