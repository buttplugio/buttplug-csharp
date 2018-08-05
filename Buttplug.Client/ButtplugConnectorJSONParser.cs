using Buttplug.Core;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    public class ButtplugConnectorJSONParser
    {
        /// <summary>
        /// Used for converting messages between JSON and Objects.
        /// </summary>
        [NotNull]
        private readonly ButtplugJsonMessageParser _parser = new ButtplugJsonMessageParser(new ButtplugLogManager());

        /// <summary>
        /// Converts a single <see cref="ButtplugMessage"/> into a JSON string.
        /// </summary>
        /// <param name="aMsg">Message to convert.</param>
        /// <returns>The JSON string representation of the message.</returns>
        public string Serialize(ButtplugMessage aMsg)
        {
            return _parser.Serialize(aMsg, ButtplugMessage.CurrentSchemaVersion);
        }

        /// <summary>
        /// Converts an array of <see cref="ButtplugMessage"/> into a JSON string.
        /// </summary>
        /// <param name="aMsgs">An array of messages to convert.</param>
        /// <returns>The JSON string representation of the messages.</returns>
        public string Serialize(ButtplugMessage[] aMsgs)
        {
            return _parser.Serialize(aMsgs, ButtplugMessage.CurrentSchemaVersion);
        }

        /// <summary>
        /// Converts a JSON string into an array of <see cref="ButtplugMessage"/>.
        /// </summary>
        /// <param name="aMsg">A JSON string representing one or more messages.</param>
        /// <returns>An array of <see cref="ButtplugMessage"/>.</returns>
        public ButtplugMessage[] Deserialize(string aMsg)
        {
            return _parser.Deserialize(aMsg);
        }
    }
}