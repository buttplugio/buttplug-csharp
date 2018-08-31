// <copyright file="ButtplugDeviceTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Server.Bluetooth;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class ButtplugBluetoothDeviceTests
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
    }
}
