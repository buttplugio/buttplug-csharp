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
        
        [Test]
        public void TestToStepValuePositive()
        {
            var p = PercentOrSteps.FromPercent(0.5);
            Assert.AreEqual(50, p.ToStepValue(-100, 100));
            
            p = PercentOrSteps.FromPercent(1.0);
            Assert.AreEqual(100, p.ToStepValue(-100, 100));

            p = PercentOrSteps.FromPercent(0.0);
            Assert.AreEqual(0, p.ToStepValue(-100, 100));
        }

        [Test]
        public void TestToStepValueNegative()
        {
            var p = PercentOrSteps.FromPercent(-0.5, -1.0);
            // Math.Floor(-0.5 * -100 * -1) = Math.Floor(-0.5 * 100) = Math.Floor(-50) = -50
            Assert.AreEqual(-50, p.ToStepValue(-100, 100));

            p = PercentOrSteps.FromPercent(-1.0, -1.0);
            Assert.AreEqual(-100, p.ToStepValue(-100, 100));
        }

        [Test]
        public void TestToStepValueAsymmetric()
        {
            var p = PercentOrSteps.FromPercent(0.555);
            Assert.AreEqual(56, p.ToStepValue(-10, 100));

            p = PercentOrSteps.FromPercent(-0.555, -1.0);
            Assert.AreEqual(-6, p.ToStepValue(-10, 100));
        }

        [Test]
        public void TestToStepValueSteps()
        {
            var s = PercentOrSteps.FromSteps(42);
            Assert.AreEqual(42, s.ToStepValue(-100, 100));
            
            s = PercentOrSteps.FromSteps(-42);
            Assert.AreEqual(-42, s.ToStepValue(-100, 100));
        }
    }
}
