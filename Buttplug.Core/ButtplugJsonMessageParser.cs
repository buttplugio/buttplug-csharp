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
    public class ButtplugJsonMessageParser
    {
        [NotNull]
        private readonly Dictionary<string, Type> _messageTypes;
        [NotNull]
        private readonly IButtplugLog _bpLogger;
        [NotNull]
        private readonly JsonSchema4 _schema;

        public ButtplugJsonMessageParser(IButtplugLogManager aLogManager = null)
        {
            _bpLogger = aLogManager.GetLogger(GetType());
            _bpLogger?.Info($"Setting up {GetType().Name}");
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
                if (!_messageTypes.Keys.Any() || !_messageTypes.Keys.Contains(msgName))
                {
                    var err = new Error($"{msgName} is not a valid message class", ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId);
                    _bpLogger?.LogErrorMsg(err);
                    res.Add(err);
                    continue;
                }

                var s = new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Error };

                // This specifically could fail due to object conversion.
                try
                {
                    var r = o[msgName].Value<JObject>();
                    res.Add((ButtplugMessage)r.ToObject(_messageTypes[msgName], s));
                    _bpLogger?.Trace($"Message successfully parsed as {msgName} type");
                }
                catch (InvalidCastException e)
                {
                    var err = new Error($"Could not create message for JSON {aJsonMsg}: {e.Message}", ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId);
                    _bpLogger?.LogErrorMsg(err);
                    res.Add(err);
                }
                catch (JsonSerializationException e)
                {
                    var err = new Error($"Could not create message for JSON {aJsonMsg}: {e.Message}", ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId);
                    _bpLogger?.LogErrorMsg(err);
                    res.Add(err);
                }
            }

            return res.ToArray();
        }

        public string Serialize([NotNull] ButtplugMessage aMsg, uint clientSchemaVersion)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.

            // Support downgrading messages
            var tmp = aMsg;
            while (tmp.MessageVersioningVersion > clientSchemaVersion)
            {
                if (tmp.MessageVersioningPrevious == null)
                {
                    if (tmp.Id == ButtplugConsts.SystemMsgId)
                    {
                        // There's no previous version of this system message
                        _bpLogger?.Warn($"No messages serialized!");
                        return null;
                    }

                    tmp = new Error($"No backwards compatible version for message #{tmp.GetType().Name}!",
                                    ErrorClass.ERROR_MSG, tmp.Id);
                    continue;
                }

                tmp = (ButtplugMessage)aMsg.MessageVersioningPrevious.GetConstructor(
                    new Type[] { tmp.GetType() }).Invoke(new object[] { tmp });
            }

            var o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(tmp)));
            var a = new JArray(o);
            _bpLogger?.Trace($"Message serialized to: {a.ToString(Formatting.None)}", true);
            return a.ToString(Formatting.None);
        }

        public string Serialize([NotNull] IEnumerable<ButtplugMessage> aMsgs, uint clientSchemaVersion)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            var a = new JArray();
            foreach (var msg in aMsgs)
            {
                // Support downgrading messages
                var tmp = msg;
                while (tmp.MessageVersioningVersion > clientSchemaVersion)
                {
                    if (tmp.MessageVersioningPrevious == null)
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

                    tmp = (ButtplugMessage)tmp.MessageVersioningPrevious.GetConstructor(
                        new Type[] { tmp.GetType() }).Invoke(new object[] { tmp });
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