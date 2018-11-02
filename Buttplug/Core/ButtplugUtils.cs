using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

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

        /// <summary>
        /// Ensures that the specified argument is not null.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="argument">The argument.</param>
        /// <remarks>https://stackoverflow.com/questions/29184887/best-way-to-check-for-null-parameters-guard-clauses</remarks>
        [DebuggerStepThrough]
        [ContractAnnotation("halt <= argument:null")]
        public static void ArgumentNotNull(object aArgument, [InvokerParameterName] string aArgumentName)
        {
            if (aArgument == null)
            {
                throw new ArgumentNullException(aArgumentName);
            }
        }

        /// <summary>
        /// Gets embedded license files in assemblies.
        /// </summary>
        /// <param name="aResourceName">Resource to retrieve</param>
        /// <returns>String of all licenses for assembly dependencies.</returns>
        // ReSharper disable once UnusedMember.Global
        public static string GetLicense(Assembly aAssembly, string aResourceName)
        {
            Stream stream = null;
            try
            {
                stream = aAssembly.GetManifestResourceStream(aResourceName);
                using (var reader = new StreamReader(stream ?? throw new InvalidOperationException()))
                {
                    stream = null;
                    return reader.ReadToEnd();
                }
            }
            finally
            {
                stream?.Dispose();
            }
        }

        /// <summary>
        /// Given a string, tries to return the ButtplugMessage Type (as in, C# Class Type) denoted by the string.
        /// </summary>
        /// <remarks>
        /// Added as part of the Buttplug.Core utils so we don't have to worry about Assembly resolution.
        /// </remarks>
        /// <param name="aMessageName">Name of the message type to find a Type for. Case-sensitive.</param>
        /// <returns>Type object of message type if it exists, otherwise null.</returns>
        public static Type GetMessageType(string aMessageName)
        {
            return Type.GetType($"Buttplug.Core.Messages.{aMessageName}");
        }
    }
}