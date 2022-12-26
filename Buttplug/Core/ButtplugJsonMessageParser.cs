// <copyright file="ButtplugJsonMessageParser.cs" company="Nonpolynomial Labs LLC">
//     Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
//     Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
//     license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Buttplug.Core.Messages;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Buttplug.Core
{
    /// <summary>
    /// Handles serialization (object to JSON), deserialization (JSON to object) and validation of messages.
    /// </summary>
    public class ButtplugJsonMessageParser
    {
        /// <summary>
        /// Map of message names to message types.
        /// </summary>
        private readonly Dictionary<string, Type> _messageTypes;

        /// <summary>
        /// Serializes/deserializes object to/from JSON.
        /// </summary>
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugJsonMessageParser"/> class.
        /// </summary>
        /// <param name="logManager">Log manager, passed from the parser owner.</param>
        public ButtplugJsonMessageParser()
        {
            _serializer = new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Error };
            _messageTypes = new Dictionary<string, Type>();
            foreach (var messageType in ButtplugUtils.GetAllMessageTypes())
            {
                _messageTypes.Add(messageType.Name, messageType);
            }

            // If we can't find any message types in our assembly, the system is basically useless.
            if (!_messageTypes.Any())
            {
                throw new ButtplugMessageException("No message types available.");
            }

            // Load the schema for validation. Schema file is an embedded resource in the library.
            var jsonSchemstring = ButtplugUtils.GetStringFromFileResource("Buttplug.buttplug-schema.json");
        }

        /// <summary>
        /// Deserializes Buttplug messages from JSON into an array of <see cref="ButtplugMessage"/> objects.
        /// </summary>
        /// <param name="jsonMsg">String containing one or more Buttplug messages in JSON format.</param>
        /// <returns>Enumerable of <see cref="ButtplugMessage"/> objects.</returns>
        public IEnumerable<ButtplugMessage> Deserialize(string jsonMsg)
        {
            var textReader = new StringReader(jsonMsg);

            // While we aren't receiving from a stream here, we may get multiple JSON arrays
            // depending on how we received messages.
            var reader = new JsonTextReader(textReader)
            {
                CloseInput = false,
                SupportMultipleContent = true,
            };

            // Aggregate all messages in the string we received. Note that we may have received
            // multiple strings, which may have multiple arrays. If any message in the array is
            // invalid, we dump the message list and just throw. This is considered a catastrophic
            // event and the system should probably just shut down anyways.
            var msgList = new List<ButtplugMessage>();
            while (true)
            {
                try
                {
                    if (!reader.Read())
                    {
                        break;
                    }
                }
                catch (JsonReaderException e)
                {
                    throw new ButtplugMessageException($"Not valid JSON: {jsonMsg} - {e.Message}");
                }

                JArray msgArray;
                try
                {
                    msgArray = JArray.Load(reader);
                }
                catch (JsonReaderException e)
                {
                    throw new ButtplugMessageException($"Not valid JSON: {jsonMsg} - {e.Message}");
                }

                foreach (var jsonObj in msgArray.Children<JObject>())
                {
                    var msgName = jsonObj.Properties().First().Name;

                    // Only way we should get here is if the schema includes a class that we don't
                    // have a matching C# class for.
                    if (!_messageTypes.ContainsKey(msgName))
                    {
                        throw new ButtplugMessageException($"{msgName} is not a valid message class");
                    }

                    // This specifically could fail due to object conversion.
                    msgList.Add(DeserializeAs(jsonObj, _messageTypes[msgName]));
                }
            }

            return msgList;
        }

        /// <summary>
        /// Take a JObject and turn it into a message type.
        /// </summary>
        /// <param name="object">JSON Object to convert to a <see cref="ButtplugMessage"/>.</param>
        /// <param name="msgType">Type of message we want to convert the JSON object to.</param>
        /// <returns>Returns a deserialized <see cref="ButtplugMessage"/>, as the requested type.</returns>
        private ButtplugMessage DeserializeAs(JObject obj, Type msgType)
        {
            if (!msgType.IsSubclassOf(typeof(ButtplugMessage)))
            {
                throw new ButtplugMessageException(
                    $"Type {msgType.Name} is not a subclass of ButtplugMessage");
            }

            var msgName = ButtplugMessage.GetName(msgType);
            try
            {
                var msgObj = obj[msgName].Value<JObject>();
                var msg = (ButtplugMessage)msgObj.ToObject(msgType, _serializer);
                return msg;
            }
            catch (InvalidCastException e)
            {
                throw new ButtplugMessageException(
                    $"Could not create message for JSON {obj}: {e.Message}");
            }
            catch (JsonSerializationException e)
            {
                // Object didn't fit. Downgrade?
                var prevType = ButtplugMessage.GetPreviousType(msgType);
                if (prevType == null)
                {
                    throw new ButtplugMessageException(
                        $"Could not create message for JSON {obj}: {e.Message}");
                }

                return DeserializeAs(obj, prevType);
            }
        }

        /// <summary>
        /// Serializes a single <see cref="ButtplugMessage"/> object into a JSON string for a
        /// specified version of the schema.
        /// </summary>
        /// <param name="msg"><see cref="ButtplugMessage"/> object.</param>
        /// <param name="clientSchemversion">Target schema version.</param>
        /// <returns>JSON string representing a Buttplug message.</returns>
        public string Serialize(ButtplugMessage msg, uint clientSchemversion)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            // Support downgrading messages

            var jsonMsg = ButtplugMessageToJObject(msg, clientSchemversion);

            // If we get nothing back, throw now, because if we don't the schema verifier will.
            if (jsonMsg == null)
            {
                throw new ButtplugMessageException(
                    "Message cannot be converted to JSON.", msg.Id);
            }

            var msgArray = new JArray { jsonMsg };

            return msgArray.ToString(Formatting.None);
        }

        /// <summary>
        /// Serializes a collection of ButtplugMessage objects into a JSON string for a specified
        /// version of the schema.
        /// </summary>
        /// <param name="msgs">A collection of ButtplugMessage objects.</param>
        /// <param name="clientSchemversion">The target schema version.</param>
        /// <returns>A JSON string representing one or more Buttplug messages.</returns>
        public string Serialize(IEnumerable<ButtplugMessage> msgs, uint clientSchemversion)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            var msgArray = new JArray();
            foreach (var msg in msgs)
            {
                var obj = ButtplugMessageToJObject(msg, clientSchemversion);
                if (obj == null)
                {
                    continue;
                }

                msgArray.Add(obj);
            }

            // If we somehow didn't encode anything, throw. Otherwise we'll try to pass a string full
            // of nothing through the schema verifier and it will throw.
            if (!msgArray.Any())
            {
                throw new ButtplugMessageException(
                    "No messages serialized.");
            }

            return msgArray.ToString(Formatting.None);
        }

        /// <summary>
        /// Given a Buttplug message and a schema version, turn the message into a JSON.Net JObject.
        /// This method can return null in some cases, which should be checked for by callers.
        /// </summary>
        /// <param name="msg">Message to convert to JSON.</param>
        /// <param name="clientSpecVersion">
        /// Schema version of the client the message will be sent to.
        /// </param>
        /// <exception cref="ButtplugMessageException">
        /// Throws when no backward compatible non-system message can be found, or when a default
        /// constructor for a message is not available.
        /// </exception>
        /// <returns>JObject on success, but can return null in cases where a system message is not compatible with a client schema.</returns>
        private JObject ButtplugMessageToJObject(ButtplugMessage msg, uint clientSpecVersion)
        {
            // Support downgrading messages
            while (msg.SpecVersion > clientSpecVersion)
            {
                if (msg.PreviousType == null)
                {
                    if (msg.Id != ButtplugConsts.SystemMsgId)
                    {
                        throw new ButtplugMessageException(
                            $"No backwards compatible version for message #{msg.Name} at Schema Version {clientSpecVersion}!", msg.Id);
                    }

                    // There's no previous version of this system message. We can't send the current
                    // version, because whoever is on the other side won't be able to parse it.
                    // However, this isn't exactly an error, because there's no real way to recover
                    // from this and it's not like it'd do any good to send it. Therefore we just log
                    // and throw the message away.
                    return null;
                }

                var newMsg = (ButtplugMessage)msg.PreviousType.GetConstructor(
                    new[] { msg.GetType() })?.Invoke(new object[] { msg });
                if (newMsg == null)
                {
                    throw new ButtplugMessageException($"No default constructor for message #{msg.GetType().Name}!",
                        msg.Id);
                }

                return ButtplugMessageToJObject(newMsg, clientSpecVersion);
            }

            return new JObject(new JProperty(msg.Name, JObject.FromObject(msg)));
        }
    }
}