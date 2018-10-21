// <copyright file="ButtplugExceptionTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Diagnostics.CodeAnalysis;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Core.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class ButtplugExceptionTests
    {
        [Test]
        public void TestFromError()
        {
            ButtplugException.FromError(new Error("test", Error.ErrorClass.ERROR_DEVICE, ButtplugConsts.SystemMsgId))
                .Should().BeOfType<ButtplugDeviceException>();
            ButtplugException.FromError(new Error("test", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId))
                .Should().BeOfType<ButtplugException>();
            ButtplugException.FromError(new Error("test", Error.ErrorClass.ERROR_INIT, ButtplugConsts.SystemMsgId))
                .Should().BeOfType<ButtplugHandshakeException>();
            ButtplugException.FromError(new Error("test", Error.ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId))
                .Should().BeOfType<ButtplugMessageException>();
            ButtplugException.FromError(new Error("test", Error.ErrorClass.ERROR_PING, ButtplugConsts.SystemMsgId))
                .Should().BeOfType<ButtplugPingException>();
        }

        [Test]
        public void TestToErrorMessage()
        {
            var msg = new Error("test1", Error.ErrorClass.ERROR_DEVICE, 2);
            var err = ButtplugException.FromError(msg);
            err.ButtplugErrorMessage.Should().BeEquivalentTo(msg);
        }
    }
}