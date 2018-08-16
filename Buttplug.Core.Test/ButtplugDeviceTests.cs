// <copyright file="ButtplugDeviceTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Core.Test
{
    [TestFixture]
    public class ButtplugDeviceTests
    {
        [Test]
        public async Task TestBaseDevice()
        {
            var log = new ButtplugLogManager();
            var dev = new TestDevice(log, "testDev")
            {
                Index = 2,
            };

            Assert.AreEqual(2, dev.Index);

            Assert.True(await dev.Initialize(default(CancellationToken)) is Ok);

            Assert.True(await dev.ParseMessage(new StopDeviceCmd(2), default(CancellationToken)) is Ok);

            var outMsg = await dev.ParseMessage(new RotateCmd(2, new List<RotateCmd.RotateSubcommand>()), default(CancellationToken));
            Assert.True(outMsg is Error);
            Assert.AreEqual(Error.ErrorClass.ERROR_DEVICE, (outMsg as Error).ErrorCode);
            Assert.True((outMsg as Error).ErrorMessage.Contains("cannot handle message of type"));

            dev.Disconnect();
            outMsg = await dev.ParseMessage(new StopDeviceCmd(2), default(CancellationToken));
            Assert.True(outMsg is Error);
            Assert.AreEqual(Error.ErrorClass.ERROR_DEVICE, (outMsg as Error).ErrorCode);
            Assert.True((outMsg as Error).ErrorMessage.Contains("has disconnected"));
        }
    }
}
