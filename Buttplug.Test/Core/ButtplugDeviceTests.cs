﻿// <copyright file="ButtplugDeviceTests.cs" company="Nonpolynomial Labs LLC">
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
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Devices;
using Buttplug.Test;
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
            var logMgr = new ButtplugLogManager();
            var devImpl = new TestDeviceImpl(logMgr, "Device");
            var dev = new ButtplugDevice(logMgr, new TestProtocol(logMgr, devImpl), devImpl)
            {
                Index = 2,
            };

            await dev.InitializeAsync(default(CancellationToken));

            (await dev.ParseMessageAsync(new StopDeviceCmd(2), default(CancellationToken))).Should().BeOfType<Ok>();

            dev.Awaiting(async device => await dev.ParseMessageAsync(new RotateCmd(2, new List<RotateCmd.RotateSubcommand>()), default(CancellationToken))).Should().Throw<ButtplugDeviceException>();

            dev.Disconnect();
            dev.Awaiting(async device => await device.ParseMessageAsync(new StopDeviceCmd(2), default(CancellationToken))).Should().Throw<ButtplugDeviceException>();
        }

        protected class TestProtocolDoubleAdd : TestProtocol
        {
            public TestProtocolDoubleAdd(ButtplugLogManager logger, IButtplugDeviceImpl device)
                : base(logger, device)
            {
                // Add HandleRotateCmd twice, should throw
                AddMessageHandler<RotateCmd>(HandleRotateCmd);
                AddMessageHandler<RotateCmd>(HandleRotateCmd);
            }

            public Task<ButtplugMessage> HandleRotateCmd(ButtplugDeviceMessage msg, CancellationToken token)
            {
                return Task.FromResult(new Ok(ButtplugConsts.SystemMsgId) as ButtplugMessage);
            }
        }

        [Test]
        public void TestFunctionDoubleAdd()
        {
            var logMgr = new ButtplugLogManager();
            Action act = () => new TestProtocolDoubleAdd(logMgr, new TestDeviceImpl(logMgr, "Test"));
            act.Should().Throw<ArgumentException>();
        }
    }
}
