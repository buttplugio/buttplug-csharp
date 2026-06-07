// <copyright file="ButtplugClientDeviceCommandTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using Buttplug.Core;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [TestFixture]
    public class ButtplugClientDeviceCommandTests
    {
        [Test]
        public void TestRotatePercentAllowsNegativeDirection()
        {
            var command = DeviceOutput.Rotate.Percent(-0.5);

            command.Value.Percent.Should().Be(-0.5);
        }

        [Test]
        public void TestNonRotatePercentRejectsNegativeValue()
        {
            DeviceOutput.Vibrate
                .Invoking(x => x.Percent(-0.5))
                .Should()
                .Throw<ButtplugDeviceException>();
        }
    }
}
