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

        public ButtplugJsonMessageParser([NotNull] IButtplugLogManager aLogManager)
        {
            _bpLogger = aLogManager.GetLogger(GetType());
            _bpLogger.Debug($"Setting up {GetType().Name}");
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

            var messageClasses = from t in allTypes
                                 where t != null && t.IsClass && t.Namespace == "Buttplug.Core.Messages" && typeof(ButtplugMessage).IsAssignableFrom(t)
                                 select t;

            var enumerable = messageClasses as Type[] ?? messageClasses.ToArray();
            _bpLogger.Debug($"Message type count: {enumerable.Length}");
            _messageTypes = new Dictionary<string, Type>();
            enumerable.ToList().ForEach(aMessageType =>
            {
                _bpLogger.Debug($"- {aMessageType.Name}");
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
            finally
            {
                stream?.Dispose();
            }
        }

        [NotNull]
        public ButtplugMessage[] Deserialize(string aJsonMsg)
        {
            _bpLogger.Trace($"Got JSON Message: {aJsonMsg}");

            var res = new List<ButtplugMessage>();
            JArray msgArray;
            try
            {
                msgArray = JArray.Parse(aJsonMsg);
            }
            catch (JsonReaderException e)
            {
                res.Add(_bpLogger.LogErrorMsg(ButtplugConsts.SystemMsgId, ErrorClass.ERROR_MSG, "Not valid JSON"));
                return res.ToArray();
            }

            var errors = _schema.Validate(msgArray);
            if (errors.Any())
            {
                res.Add(_bpLogger.LogErrorMsg(ButtplugConsts.SystemMsgId, ErrorClass.ERROR_MSG, "Message does not conform to schema: " + string.Join(", ", errors.Select(aErr => aErr.ToString()).ToArray())));
                return res.ToArray();
            }

            if (!msgArray.Any())
            {
                res.Add(_bpLogger.LogErrorMsg(ButtplugConsts.SystemMsgId, ErrorClass.ERROR_MSG, "No messages in array"));
                return res.ToArray();
            }

            // JSON input is an array of messages.
            // We currently only handle the first one.
            foreach (var o in msgArray.Children<JObject>())
            {
                if (!o.Properties().Any())
                {
                    res.Add(_bpLogger.LogErrorMsg(ButtplugConsts.SystemMsgId, ErrorClass.ERROR_MSG, "No message name available"));
                    continue;
                }

                var msgName = o.Properties().First().Name;
                if (!_messageTypes.Keys.Any() || !_messageTypes.Keys.Contains(msgName))
                {
                    res.Add(_bpLogger.LogErrorMsg(ButtplugConsts.SystemMsgId, ErrorClass.ERROR_MSG, $"{msgName} is not a valid message class"));
                    continue;
                }

                var s = new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Error };

                // This specifically could fail due to object conversion.
                try
                {
                    var r = o[msgName].Value<JObject>();
                    res.Add((ButtplugMessage)r.ToObject(_messageTypes[msgName], s));
                    _bpLogger.Trace($"Message successfully parsed as {msgName} type");
                }
                catch (InvalidCastException e)
                {
                    res.Add(_bpLogger.LogErrorMsg(ButtplugConsts.SystemMsgId, ErrorClass.ERROR_MSG, $"Could not create message for JSON {aJsonMsg}: {e.Message}"));
                }
                catch (JsonSerializationException e)
                {
                    res.Add(_bpLogger.LogErrorMsg(ButtplugConsts.SystemMsgId, ErrorClass.ERROR_MSG, $"Could not create message for JSON {aJsonMsg}: {e.Message}"));
                }
            }

            return res.ToArray();
        }

        public string Serialize([NotNull] ButtplugMessage aMsg)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            var o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(aMsg)));
            var a = new JArray(o);
            _bpLogger.Trace($"Message serialized to: {a.ToString(Formatting.None)}", true);
            return a.ToString(Formatting.None);
        }

        public string Serialize([NotNull] IEnumerable<ButtplugMessage> aMsgs)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            var a = new JArray();
            foreach (var msg in aMsgs)
            {
                var o = new JObject(new JProperty(msg.GetType().Name, JObject.FromObject(msg)));
                a.Add(o);
            }

            _bpLogger.Trace($"Message serialized to: {a.ToString(Formatting.None)}", true);
            return a.ToString(Formatting.None);
        }
    }
}