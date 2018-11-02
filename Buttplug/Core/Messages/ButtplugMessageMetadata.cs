using System;

namespace Buttplug.Core.Messages
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ButtplugMessageMetadata : Attribute
    {
        /// <summary>
        /// String name of the message. Can be similar between different spec versions of messages.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Current message spec version. History of versions can be seen at https://github.com/metafetish/buttplug-spec.
        /// </summary>
        public uint Version { get; }

        /// <summary>
        /// Previous message type, if the message has changed between schema versions. Can be null.
        /// </summary>
        public Type PreviousType { get; }

        public ButtplugMessageMetadata(string aName, uint aSpecVersion, Type aPreviousType = null)
        {
            Name = aName;
            Version = aSpecVersion;
            PreviousType = aPreviousType;
        }
    }
}