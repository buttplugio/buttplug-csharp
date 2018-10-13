using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Buttplug.Core.Devices;
using Buttplug.Core.Messages;

namespace Buttplug.Core
{
    public class ButtplugUtils
    {
        /// <summary>
        /// Returns all ButtplugMessage deriving types in the assembly the core library is linked to.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllMessageTypes()
        {
            IEnumerable<Type> allTypes;
            // Some classes in the library may not load on certain platforms due to missing symbols.
            // If this is the case, we should still find messages even though an exception was thrown.
            try
            {
                allTypes = Assembly.GetAssembly(typeof(ButtplugMessage))?.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                allTypes = e.Types;
            }

            // Classes should derive from ButtplugMessage. ButtplugDeviceMessage is a special generic case.
            return (allTypes ?? throw new InvalidOperationException())
                    .Where(aType => aType != null &&
                                    aType.IsClass &&
                                    aType.IsSubclassOf(typeof(ButtplugMessage)) &&
                                    aType != typeof(ButtplugDeviceMessage));
        }
    }
}