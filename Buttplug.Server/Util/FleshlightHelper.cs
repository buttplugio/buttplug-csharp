using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buttplug.Server.Util
{
    /// <summary>
    /// Fleshlight speed calculation utilities
    /// </summary>
    public class FleshlightHelper
    {
        /// <summary>
        /// Speed returns the distance (in percent) moved given speed (in percent)
        /// in the given duration (milliseconds).
        /// Thanks to @funjack - https://github.com/funjack/launchcontrol/blob/master/protocol/funscript/functions.go
        /// </summary>
        /// <param name="aDuration">The time to move in milliseconds</param>
        /// <param name="aSpeed">The speed as a percentage (0.0-1.0)</param>
        /// <returns>The distance as a percentage (0.0-1.0)</returns>
        public static double GetDistance(uint aDuration, double aSpeed)
        {
            if (aSpeed <= 0)
            {
                return 0;
            }
            else if (aSpeed > 1)
            {
                aSpeed = 1;
            }

            var mil = Math.Pow(aSpeed / 250, -0.95);
            var diff = mil - Convert.ToDouble(aDuration);
            return Math.Abs(diff) < 0.001 ? 0 :
                Math.Max(Math.Min(Convert.ToDouble(90 - (diff / mil * 90)) / 100, 1), 0);
        }

        /// <summary>
        /// Speed returns the speed (in percent) to move the given distance (in percent)
        /// in the given duration (milliseconds).
        /// Thanks to @funjack - https://github.com/funjack/launchcontrol/blob/master/protocol/funscript/functions.go
        /// </summary>
        /// <param name="aDistance">The distance as a percentage (0.0-1.0)</param>
        /// <param name="aDuration">The time to move in milliseconds</param>
        /// <returns>The speed as a percentage (0.0-1.0)</returns>
        public static double GetSpeed(double aDistance, uint aDuration)
        {
            if (aDistance <= 0)
            {
                return 0;
            }
            else if (aDistance > 1)
            {
                aDistance = 1;
            }

            return 250 * Math.Pow((aDuration * 90) / (aDistance * 100), -1.05);
        }

        /// <summary>
        /// Duration returns the time it will take to move the given distance (in
        /// percent) at the given speed (in percent).
        /// </summary>
        /// <param name="aDistance">The distance as a percentage (0.0-1.0)</param>
        /// <param name="aSpeed">The speed as a percentage (0.0-1.0)</param>
        /// <returns>The time it will take to move in milliseconds</returns>
        public static double GetDuration(double aDistance, double aSpeed)
        {
            if (aDistance <= 0)
            {
                return 0;
            }
            else if (aDistance > 1)
            {
                aDistance = 1;
            }

            if (aSpeed <= 0)
            {
                return 0;
            }
            else if (aSpeed > 1)
            {
                aSpeed = 1;
            }

            var mil = Math.Pow(aSpeed / 250, -0.95);
            return mil / (90 / (aDistance * 100));
        }
    }
}
