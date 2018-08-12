// <copyright file="ButtplugDeviceTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class ButtplugDeviceTests
    {
        [Test]
        public void TestBuiltinDeviceLoading()
        {
            var buttplugAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .SingleOrDefault(aAssembly => aAssembly.GetName().Name == "Buttplug.Server");
            Assert.NotNull(buttplugAssembly);
            var types = buttplugAssembly.GetTypes()
                .Where(aType => aType.IsClass && aType.Namespace == "Buttplug.Server.Bluetooth.Devices" &&
                            typeof(IBluetoothDeviceInfo).IsAssignableFrom(aType)).ToList();
            Assert.True(types.Any());
            var b = new TestBluetoothSubtypeManager(new ButtplugLogManager());
            var d = b.GetDefaultDeviceInfoList();
            foreach (var t in types)
            {
                Assert.True(d.Any(aInfoObj => aInfoObj.GetType() == t), $"Default types contains type: {t.Name}");
            }
        }

        [Test]
        public void TestBaseDevice()
        {
            var log = new ButtplugLogManager();
            var dev = new TestDevice(log, "testDev")
            {
                Index = 2,
            };

            Assert.AreEqual(2, dev.Index);

            Assert.True(dev.Initialize(default(CancellationToken)).GetAwaiter().GetResult() is Ok);

            Assert.True(dev.ParseMessage(new StopDeviceCmd(2), default(CancellationToken)).GetAwaiter().GetResult() is Ok);

            var outMsg = dev.ParseMessage(new RotateCmd(2, new List<RotateCmd.RotateSubcommand>()), default(CancellationToken)).GetAwaiter().GetResult();
            Assert.True(outMsg is Error);
            Assert.AreEqual(Error.ErrorClass.ERROR_DEVICE, (outMsg as Error).ErrorCode);
            Assert.True((outMsg as Error).ErrorMessage.Contains("cannot handle message of type"));

            dev.Disconnect();
            outMsg = dev.ParseMessage(new StopDeviceCmd(2), default(CancellationToken)).GetAwaiter().GetResult();
            Assert.True(outMsg is Error);
            Assert.AreEqual(Error.ErrorClass.ERROR_DEVICE, (outMsg as Error).ErrorCode);
            Assert.True((outMsg as Error).ErrorMessage.Contains("has disconnected"));
        }
    }
}
