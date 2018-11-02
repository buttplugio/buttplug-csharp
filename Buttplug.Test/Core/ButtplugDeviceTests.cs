// <copyright file="ButtplugDeviceTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Core.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class ButtplugDeviceTests
    {
        [Test]
        public async Task TestBaseDevice()
        {
            var dev = new TestDevice(new ButtplugLogManager(), "testDev")
            {
                Index = 2,
            };

            (await dev.InitializeAsync(default(CancellationToken))).Should().BeOfType<Ok>();

            (await dev.ParseMessageAsync(new StopDeviceCmd(2), default(CancellationToken))).Should().BeOfType<Ok>();

            dev.Awaiting(async aDevice => await dev.ParseMessageAsync(new RotateCmd(2, new List<RotateCmd.RotateSubcommand>()), default(CancellationToken))).Should().Throw<ButtplugDeviceException>();

            dev.Disconnect();
            dev.Awaiting(async aDevice => await aDevice.ParseMessageAsync(new StopDeviceCmd(2), default(CancellationToken))).Should().Throw<ButtplugDeviceException>();
        }

        protected class TestDeviceDoubleAdd : TestDevice
        {
            public TestDeviceDoubleAdd(ButtplugLogManager aLogger)
                : base(aLogger, "DoubleAdd")
            {
            }

            public void DoubleAdd()
            {
                // Add HandleRotateCmd twice, should throw
                AddMessageHandler<RotateCmd>(HandleRotateCmd);
                AddMessageHandler<RotateCmd>(HandleRotateCmd);
            }

            public Task<ButtplugMessage> HandleRotateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
            {
                return Task.FromResult(new Ok(ButtplugConsts.SystemMsgId) as ButtplugMessage);
            }
        }

        [Test]
        public void TestFunctionDoubleAdd()
        {
            var device = new TestDeviceDoubleAdd(new ButtplugLogManager());
            Action act = () => device.DoubleAdd();
            act.Should().Throw<ArgumentException>();
        }
    }
}
