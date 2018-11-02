// <copyright file="FleshlightHelperTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Server.Util;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class FleshlightHelperTests
    {
        [Test]
        public void TestRoundTrip()
        {
            var delta = FleshlightHelper.GetDistance(100, 0.5);
            var speed = FleshlightHelper.GetSpeed(delta, 100);
            var time = FleshlightHelper.GetDuration(delta, 0.5);
            (Math.Abs(0.5 - speed) < 0.01).Should().BeTrue();
            (Math.Abs(100 - time) < 0.01).Should().BeTrue();

            var speed2 = FleshlightHelper.GetSpeed(0.5, 500);
            var delta2 = FleshlightHelper.GetDistance(500, speed2);
            var time2 = FleshlightHelper.GetDuration(0.5, speed2);
            (Math.Abs(0.5 - delta2) < 0.01).Should().BeTrue();
            (Math.Abs(500 - time2) < 10).Should().BeTrue();

            var time3 = Convert.ToUInt32(FleshlightHelper.GetDuration(0.5, 0.5));
            var speed3 = FleshlightHelper.GetSpeed(0.5, time3);
            var delta3 = FleshlightHelper.GetDistance(time3, 0.5);
            (Math.Abs(0.5 - delta3) < 0.01).Should().BeTrue();
            (Math.Abs(0.5 - speed3) < 0.01).Should().BeTrue();
        }

        [Test]
        public void TestOutOfBounds()
        {
            (Math.Abs(FleshlightHelper.GetSpeed(0, 500) - FleshlightHelper.GetSpeed(-1, 500)) < 0.0001).Should().BeTrue();
            (Math.Abs(FleshlightHelper.GetSpeed(1, 500) - FleshlightHelper.GetSpeed(2, 500)) < 0.0001).Should().BeTrue();
            (Math.Abs(FleshlightHelper.GetDistance(100, 0) - FleshlightHelper.GetDistance(100, -1)) < 0.0001).Should().BeTrue();
            (Math.Abs(FleshlightHelper.GetDistance(100, 1) - FleshlightHelper.GetDistance(100, 2)) < 0.0001).Should().BeTrue();
            (Math.Abs(FleshlightHelper.GetDistance(1000, 0.17379819904439015016403395523936)) < 0.0001).Should().BeTrue();
            (Math.Abs(FleshlightHelper.GetDuration(0.5, 0) - FleshlightHelper.GetDuration(0.5, -1)) < 0.0001).Should().BeTrue();
            (Math.Abs(FleshlightHelper.GetDuration(0.5, 1) - FleshlightHelper.GetDuration(0.5, 2)) < 0.0001).Should().BeTrue();
            (Math.Abs(FleshlightHelper.GetDuration(0, 0.5) - FleshlightHelper.GetDuration(-1, 0.5)) < 0.0001).Should().BeTrue();
            (Math.Abs(FleshlightHelper.GetDuration(1, 0.5) - FleshlightHelper.GetDuration(2, 0.5)) < 0.0001).Should().BeTrue();
        }
    }
}
