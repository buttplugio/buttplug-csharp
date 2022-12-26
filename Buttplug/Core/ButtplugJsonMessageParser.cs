﻿// <copyright file="ButtplugJsonMessageParser.cs" company="Nonpolynomial Labs LLC">
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
        /// <param name="aLogManager">Log manager, passed from the parser owner.</param>
        public ButtplugJsonMessageParser()
        {
            this._serializer = new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Error };
            this._messageTypes = new Dictionary<string, Type>();
            foreach (var aMessageType in ButtplugUtils.GetAllMessageTypes())
            {
                this._messageTypes.Add(aMessageType.Name, aMessageType);
            }

            // If we can't find any message types in our assembly, the system is basically useless.
            if (!this._messageTypes.Any())
            {
                throw new ButtplugMessageException("No message types available.");
            }

            // Load the schema for validation. Schema file is an embedded resource in the library.
            var jsonSchemaString = ButtplugUtils.GetStringFromFileResource("Buttplug.buttplug-schema.json");
        }

        /// <summary>
        /// Deserializes Buttplug messages from JSON into an array of <see cref="ButtplugMessage"/> objects.
        /// </summary>
        /// <param name="aJsonMsg">String containing one or more Buttplug messages in JSON format.</param>
        /// <returns>Enumerable of <see cref="ButtplugMessage"/> objects.</returns>
        public IEnumerable<ButtplugMessage> Deserialize(string aJsonMsg)
        {
            var textReader = new StringReader(aJsonMsg);

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
                    throw new ButtplugMessageException($"Not valid JSON: {aJsonMsg} - {e.Message}");
                }

                JArray msgArray;
                try
                {
                    msgArray = JArray.Load(reader);
                }
                catch (JsonReaderException e)
                {
                    throw new ButtplugMessageException($"Not valid JSON: {aJsonMsg} - {e.Message}");
                }

                foreach (var jsonObj in msgArray.Children<JObject>())
                {
                    var msgName = jsonObj.Properties().First().Name;

                    // Only way we should get here is if the schema includes a class that we don't
                    // have a matching C# class for.
                    if (!this._messageTypes.ContainsKey(msgName))
                    {
                        throw new ButtplugMessageException($"{msgName} is not a valid message class");
                    }

                    // This specifically could fail due to object conversion.
                    msgList.Add(this.DeserializeAs(jsonObj, this._messageTypes[msgName]));
                }
            }

            return msgList;
        }

        /// <summary>
        /// Take a JObject and turn it into a message type.
        /// </summary>
        /// <param name="aObject">JSON Object to convert to a <see cref="ButtplugMessage"/>.</param>
        /// <param name="aMsgType">Type of message we want to convert the JSON object to.</param>
        /// <returns>Returns a deserialized <see cref="ButtplugMessage"/>, as the requested type.</returns>
        private ButtplugMessage DeserializeAs(JObject aObject, Type aMsgType)
        {
            if (!aMsgType.IsSubclassOf(typeof(ButtplugMessage)))
            {
                throw new ButtplugMessageException(
                    $"Type {aMsgType.Name} is not a subclass of ButtplugMessage");
            }

            var msgName = ButtplugMessage.GetName(aMsgType);
            try
            {
                var msgObj = aObject[msgName].Value<JObject>();
                var msg = (ButtplugMessage)msgObj.ToObject(aMsgType, this._serializer);
                return msg;
            }
            catch (InvalidCastException e)
            {
                throw new ButtplugMessageException(
                    $"Could not create message for JSON {aObject}: {e.Message}");
            }
            catch (JsonSerializationException e)
            {
                // Object didn't fit. Downgrade?
                var prevType = ButtplugMessage.GetPreviousType(aMsgType);
                if (prevType == null)
                {
                    throw new ButtplugMessageException(
                        $"Could not create message for JSON {aObject}: {e.Message}");
                }

                return this.DeserializeAs(aObject, prevType);
            }
        }

        /// <summary>
        /// Serializes a single <see cref="ButtplugMessage"/> object into a JSON string for a
        /// specified version of the schema.
        /// </summary>
        /// <param name="aMsg"><see cref="ButtplugMessage"/> object.</param>
        /// <param name="aClientSchemaVersion">Target schema version.</param>
        /// <returns>JSON string representing a Buttplug message.</returns>
        public string Serialize(ButtplugMessage aMsg, uint aClientSchemaVersion)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            // Support downgrading messages

            var jsonMsg = this.ButtplugMessageToJObject(aMsg, aClientSchemaVersion);

            // If we get nothing back, throw now, because if we don't the schema verifier will.
            if (jsonMsg == null)
            {
                throw new ButtplugMessageException(
                    "Message cannot be converted to JSON.", aMsg.Id);
            }

            var msgArray = new JArray { jsonMsg };

            return msgArray.ToString(Formatting.None);
        }

        /// <summary>
        /// Serializes a collection of ButtplugMessage objects into a JSON string for a specified
        /// version of the schema.
        /// </summary>
        /// <param name="aMsgs">A collection of ButtplugMessage objects.</param>
        /// <param name="aClientSchemaVersion">The target schema version.</param>
        /// <returns>A JSON string representing one or more Buttplug messages.</returns>
        public string Serialize(IEnumerable<ButtplugMessage> aMsgs, uint aClientSchemaVersion)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            var msgArray = new JArray();
            foreach (var msg in aMsgs)
            {
                var obj = this.ButtplugMessageToJObject(msg, aClientSchemaVersion);
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
        /// <param name="aMsg">Message to convert to JSON.</param>
        /// <param name="aClientSpecVersion">
        /// Schema version of the client the message will be sent to.
        /// </param>
        /// <exception cref="ButtplugMessageException">
        /// Throws when no backward compatible non-system message can be found, or when a default
        /// constructor for a message is not available.
        /// </exception>
        /// <returns>JObject on success, but can return null in cases where a system message is not compatible with a client schema.</returns>
        private JObject ButtplugMessageToJObject(ButtplugMessage aMsg, uint aClientSpecVersion)
        {
            // Support downgrading messages
            while (aMsg.SpecVersion > aClientSpecVersion)
            {
                if (aMsg.PreviousType == null)
                {
                    if (aMsg.Id != ButtplugConsts.SystemMsgId)
                    {
                        throw new ButtplugMessageException(
                            $"No backwards compatible version for message #{aMsg.Name} at Schema Version {aClientSpecVersion}!", aMsg.Id);
                    }

                    // There's no previous version of this system message. We can't send the current
                    // version, because whoever is on the other side won't be able to parse it.
                    // However, this isn't exactly an error, because there's no real way to recover
                    // from this and it's not like it'd do any good to send it. Therefore we just log
                    // and throw the message away.
                    return null;
                }

                var newMsg = (ButtplugMessage)aMsg.PreviousType.GetConstructor(
                    new[] { aMsg.GetType() })?.Invoke(new object[] { aMsg });
                if (newMsg == null)
                {
                    throw new ButtplugMessageException($"No default constructor for message #{aMsg.GetType().Name}!",
                        aMsg.Id);
                }

                return this.ButtplugMessageToJObject(newMsg, aClientSpecVersion);
            }

            return new JObject(new JProperty(aMsg.Name, JObject.FromObject(aMsg)));
        }
    }
}