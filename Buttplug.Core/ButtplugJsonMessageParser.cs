using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Buttplug.Core.Messages;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using static Buttplug.Core.Messages.Error;

namespace Buttplug.Core
{
    /// <summary>
    /// Handles the seralization (object to JSON), deserialization (JSON to object) and validation of messages.
    /// </summary>
    public class ButtplugJsonMessageParser
    {
        [NotNull]
        private readonly Dictionary<string, Type> _messageTypes;

        private readonly IButtplugLog _bpLogger;

        [NotNull]
        private readonly JsonSchema4 _schema;

        [NotNull]
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugJsonMessageParser"/> class.
        /// </summary>
        /// <param name="aLogManager">Log manager</param>
        public ButtplugJsonMessageParser(IButtplugLogManager aLogManager = null)
        {
            _bpLogger = aLogManager.GetLogger(GetType());
            _bpLogger?.Info($"Setting up {GetType().Name}");
            _serializer = new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Error };
            IEnumerable<Type> allTypes;

            // Some classes in the library may not load on certain platforms due to missing symbols.
            // If this is the case, we should still find messages even though an exception was thrown.
            try
            {
                allTypes = Assembly.GetAssembly(typeof(ButtplugMessage)).GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                allTypes = e.Types;
            }

            var messageClasses = allTypes.Where(t => t != null && t.IsClass && t.Namespace == "Buttplug.Core.Messages" && typeof(ButtplugMessage).IsAssignableFrom(t));

            var enumerable = messageClasses as Type[] ?? messageClasses.ToArray();
            _bpLogger?.Debug($"Message type count: {enumerable.Length}");
            _messageTypes = new Dictionary<string, Type>();
            enumerable.ToList().ForEach(aMessageType =>
            {
                _bpLogger?.Debug($"- {aMessageType.Name}");
                _messageTypes.Add(aMessageType.Name, aMessageType);
            });

            // Load the schema for validation
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "Buttplug.Core.buttplug-schema.json";
            Stream stream = null;
            try
            {
                stream = assembly.GetManifestResourceStream(resourceName);
                using (var reader = new StreamReader(stream))
                {
                    stream = null;
                    var result = reader.ReadToEnd();
                    _schema = JsonSchema4.FromJsonAsync(result).GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                _bpLogger.LogException(e);
                throw e;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        /// <summary>
        /// Deserializes Buttplug messages from JSON into an array of <see cref="ButtplugMessage"/> objects.
        /// </summary>
        /// <param name="aJsonMsg">String containing one or more Buttplug messages in JSON format</param>
        /// <returns>Array of <see cref="ButtplugMessage"/> objects</returns>
        [NotNull]
        public ButtplugMessage[] Deserialize(string aJsonMsg)
        {
            _bpLogger?.Trace($"Got JSON Message: {aJsonMsg}");

            var res = new List<ButtplugMessage>();
            JArray msgArray;
            try
            {
                msgArray = JArray.Parse(aJsonMsg);
            }
            catch (JsonReaderException e)
            {
                var err = new Error($"Not valid JSON: {aJsonMsg} - {e.Message}", ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId);
                _bpLogger?.LogErrorMsg(err);
                res.Add(err);
                return res.ToArray();
            }

            var errors = _schema.Validate(msgArray);
            if (errors.Any())
            {
                var err = new Error("Message does not conform to schema: " + string.Join(", ", errors.Select(aErr => aErr.ToString()).ToArray()), ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId);
                _bpLogger?.LogErrorMsg(err);
                res.Add(err);
                return res.ToArray();
            }

            if (!msgArray.Any())
            {
                var err = new Error("No messages in array", ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId);
                _bpLogger?.LogErrorMsg(err);
                res.Add(err);
                return res.ToArray();
            }

            // JSON input is an array of messages.
            // We currently only handle the first one.
            foreach (var o in msgArray.Children<JObject>())
            {
                if (!o.Properties().Any())
                {
                    var err = new Error("No message name available", ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId);
                    _bpLogger?.LogErrorMsg(err);
                    res.Add(err);
                    continue;
                }

                var msgName = o.Properties().First().Name;
                if (!_messageTypes.Any() || !_messageTypes.ContainsKey(msgName))
                {
                    var err = new Error($"{msgName} is not a valid message class", ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId);
                    _bpLogger?.LogErrorMsg(err);
                    res.Add(err);
                    continue;
                }

                // This specifically could fail due to object conversion.
                res.Add(DeserializeAs(o, _messageTypes[msgName], msgName, aJsonMsg));
            }

            return res.ToArray();
        }

        private ButtplugMessage DeserializeAs(JObject aObject, Type aMsgType, string aMsgName, string aJsonMsg)
        {
            try
            {
                var r = aObject[aMsgName].Value<JObject>();
                var msg = (ButtplugMessage)r.ToObject(aMsgType, _serializer);
                _bpLogger?.Trace($"Message successfully parsed as {aMsgName} type");
                return msg;
            }
            catch (InvalidCastException e)
            {
                var err = new Error($"Could not create message for JSON {aJsonMsg}: {e.Message}", ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId);
                _bpLogger?.LogErrorMsg(err);
                return err;
            }
            catch (JsonSerializationException e)
            {
                // Object didn't fit. Downgrade?
                var tmp = (ButtplugMessage)aMsgType.GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Type.EmptyTypes, null)?.Invoke(null);
                if (tmp?.PreviousType != null)
                {
                    var msg = DeserializeAs(aObject, tmp.PreviousType, aMsgName, aJsonMsg);
                    if (!(msg is Error))
                    {
                        return msg;
                    }
                }

                var err = new Error($"Could not create message for JSON {aJsonMsg}: {e.Message}", ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId);
                _bpLogger?.LogErrorMsg(err);
                return err;
            }
        }

        /// <summary>
        /// Serializes a single <see cref="ButtplugMessage"/> object into a JSON string for a specified version of the schema.
        /// </summary>
        /// <param name="aMsg"><see cref="ButtplugMessage"/> object</param>
        /// <param name="clientSchemaVersion">Target schema version</param>
        /// <returns>JSON string representing a Buttplug message</returns>
        public string Serialize([NotNull] ButtplugMessage aMsg, uint clientSchemaVersion)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.

            // Support downgrading messages
            var tmp = aMsg;
            while (tmp == null || tmp.SchemaVersion > clientSchemaVersion)
            {
                if (tmp?.PreviousType == null)
                {
                    if (aMsg.Id == ButtplugConsts.SystemMsgId)
                    {
                        // There's no previous version of this system message
                        _bpLogger?.Warn($"No messages serialized!");
                        return null;
                    }

                    var err = new Error($"No backwards compatible version for message #{aMsg.GetType().Name}!",
                                    ErrorClass.ERROR_MSG, aMsg.Id);
                    var eo = new JObject(new JProperty(err.GetType().Name, JObject.FromObject(err)));
                    var ea = new JArray(eo);
                    _bpLogger?.Error(err.ErrorMessage, true);
                    _bpLogger?.Trace($"Message serialized to: {ea.ToString(Formatting.None)}", true);
                    return ea.ToString(Formatting.None);
                }

                tmp = (ButtplugMessage)aMsg.PreviousType.GetConstructor(
                    new Type[] { tmp.GetType() })?.Invoke(new object[] { tmp });
            }

            var o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(tmp)));
            var a = new JArray(o);
            _bpLogger?.Trace($"Message serialized to: {a.ToString(Formatting.None)}", true);
            return a.ToString(Formatting.None);
        }

        /// <summary>
        /// Serializes a collection of ButtplugMessage objects into a JSON string for a specified version of the schema.
        /// </summary>
        /// <param name="aMsgs">A collection of ButtplugMessage objects</param>
        /// <param name="clientSchemaVersion">The target schema version</param>
        /// <returns>A JSON string representing one or more Buttplug messages</returns>
        public string Serialize([NotNull] IEnumerable<ButtplugMessage> aMsgs, uint clientSchemaVersion)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            var a = new JArray();
            foreach (var msg in aMsgs)
            {
                // Support downgrading messages
                var tmp = msg;
                while (tmp.SchemaVersion > clientSchemaVersion)
                {
                    if (tmp.PreviousType == null)
                    {
                        if (tmp.Id == ButtplugConsts.SystemMsgId)
                        {
                            // There's no previous version of this system message
                            continue;
                        }

                        tmp = new Error($"No backwards compatible version for message #{tmp.GetType().Name}!",
                                        ErrorClass.ERROR_MSG, tmp.Id);
                        continue;
                    }

                    tmp = (ButtplugMessage)tmp.PreviousType.GetConstructor(
                              new[] { tmp.GetType() })?.Invoke(new object[] { tmp }) ??
                          new Error($"No default constructor for message #{tmp.GetType().Name}!",
                              ErrorClass.ERROR_MSG, msg.Id);
                }

                var o = new JObject(new JProperty(msg.GetType().Name, JObject.FromObject(tmp)));
                a.Add(o);
            }

            if (!a.Any())
            {
                _bpLogger?.Warn($"No messages serialized!");
                return null;
            }

            _bpLogger?.Trace($"Message serialized to: {a.ToString(Formatting.None)}", true);
            return a.ToString(Formatting.None);
        }
    }
}