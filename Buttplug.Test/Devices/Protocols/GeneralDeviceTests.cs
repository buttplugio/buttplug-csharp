// <copyright file="GeneralDeviceTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Test.Devices.Protocols
{
    // Each of the tests in this class is run on every bluetooth device class we know of, as listed
    // in the BluetoothSubtypeManager. Saves having to repeat the code on every device specific test,
    // at the cost of some gnarly reflection.
    //
    // Note that tests that happen here only run on one kind of device from a device info class,
    // assuming all devices will act the same. If you need varying reactions per device, put those
    // tests in the specific device test class.
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class GeneralDeviceTests
    {
        // Test for existence of StopDeviceCmd on Device class, and that it noops if it's the first
        // thing sent (since we have nothing to stop).
        /*
        [Test]
        public async Task TestStopDeviceCmd()
        {
            var mgr = new TestBluetoothSubtypeManager(new ButtplugLogManager());
            foreach (var devInfo in mgr.GetDefaultDeviceInfoList())
            {
                await GetInterfaceObj(devInfo).TestDeviceMessageNoop(new StopDeviceCmd(1));
            }
        }
        */
    }
}
