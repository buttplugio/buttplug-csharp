using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

        /// <summary>
        /// Ensures that the specified argument is not null.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="argument">The argument.</param>
        /// <remarks>https://stackoverflow.com/questions/29184887/best-way-to-check-for-null-parameters-guard-clauses.</remarks>
        [DebuggerStepThrough]
        public static void ArgumentNotNull(object aArgument, string aArgumentName)
        {
            if (aArgument == null)
            {
                throw new ArgumentNullException(aArgumentName);
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

        public static string GetStringFromFileResource(string aResourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(aResourceName);
            string result;
            try
            {
                using (var reader = new StreamReader(stream ?? throw new InvalidOperationException()))
                {
                    stream = null;
                    result = reader.ReadToEnd();
                }
            }
            finally
            {
                // Always make sure we dispose of the resource stream, even if we throw. All
                // exceptions should be rethrown though.
                stream?.Dispose();
            }

            return result;
        }
    }
}
