using System;
using Buttplug.Server.Util;
using Xunit;

namespace Buttplug.Server.Test
{
    public class FleshlightHelperTests
    {
        [Fact]
        public void TestRoundTrip()
        {
            var delta = FleshlightHelper.GetDistance(100, 0.5);
            var speed = FleshlightHelper.GetSpeed(delta, 100);
            var time = FleshlightHelper.GetDuration(delta, 0.5);
            Assert.True(Math.Abs(0.5 - speed) < 0.01);
            Assert.True(Math.Abs(100 - time) < 0.01);

            var speed2 = FleshlightHelper.GetSpeed(0.5, 500);
            var delta2 = FleshlightHelper.GetDistance(500, speed2);
            var time2 = FleshlightHelper.GetDuration(0.5, speed2);
            Assert.True(Math.Abs(0.5 - delta2) < 0.01);
            Assert.True(Math.Abs(500 - time2) < 10);

            var time3 = Convert.ToUInt32(FleshlightHelper.GetDuration(0.5, 0.5));
            var speed3 = FleshlightHelper.GetSpeed(0.5, time3);
            var delta3 = FleshlightHelper.GetDistance(time3, 0.5);
            Assert.True(Math.Abs(0.5 - delta3) < 0.01);
            Assert.True(Math.Abs(0.5 - speed3) < 0.01);
        }
    }
}
