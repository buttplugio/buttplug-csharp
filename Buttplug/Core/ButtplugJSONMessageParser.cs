using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Buttplug.Messages;
using Newtonsoft.Json.Schema;
using System.IO;

namespace Buttplug.Core
{
    internal class ButtplugJsonMessageParser
    {
        [NotNull]
        private readonly Dictionary<string, Type> _messageTypes;
        [NotNull]
        private readonly IButtplugLog _bpLogger;
        [NotNull]
        private readonly JSchema _schema;

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
                                 where t != null && t.IsClass && t.Namespace == "Buttplug.Messages" && typeof(ButtplugMessage).IsAssignableFrom(t)
                                 select t;

            var enumerable = messageClasses as Type[] ?? messageClasses.ToArray();
            _bpLogger.Debug($"Message type count: {enumerable.Count()}");
            _messageTypes = new Dictionary<string, Type>();
            enumerable.ToList().ForEach(c =>
            {
                _bpLogger.Debug($"- {c.Name}");
                _messageTypes.Add(c.Name, c);
            });

            // Load the schema for validation
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Buttplug.buttplug-schema.json";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                _schema = JSchema.Parse(result);
            }
        }

        [NotNull]
        public ButtplugMessage[] Deserialize(string aJsonMsg)
        {
            _bpLogger.Trace($"Got JSON Message: {aJsonMsg}");

            List<ButtplugMessage> res = new List<ButtplugMessage>();
            JArray a;
            try
            {
                a = JArray.Parse(aJsonMsg);
            }
            catch (JsonReaderException e)
            {
                _bpLogger.Debug($"Not valid JSON: {aJsonMsg}");
                _bpLogger.Debug(e.Message);
                res.Add(new Error("Not valid JSON", ButtplugConsts.SYSTEM_MSG_ID));
                return res.ToArray(); 
            }

            IList<string> errors = new List<string>();
            if (!a.IsValid(_schema, out errors))
            {
                res.Add(new Error("JSON does not conform to schema! Errors: " + String.Join(", ", errors.ToArray()), ButtplugConsts.SYSTEM_MSG_ID));
                return res.ToArray();
            }

            if (!a.Any())
            {
                res.Add(new Error("No messages in array", ButtplugConsts.SYSTEM_MSG_ID));
                return res.ToArray();
            }

            // JSON input is an array of messages.
            // We currently only handle the first one.

            foreach (JObject o in a.Children<JObject>())
            {
                if (!o.Properties().Any())
                {
                    res.Add(new Error("No message name available", ButtplugConsts.SYSTEM_MSG_ID));
                    continue;
                }
                var msgName = o.Properties().First().Name;
                if (!_messageTypes.Keys.Any() || !_messageTypes.Keys.Contains(msgName))
                {
                    res.Add(new Error($"{msgName} is not a valid message class", ButtplugConsts.SYSTEM_MSG_ID));
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
                    res.Add(_bpLogger.LogErrorMsg(ButtplugConsts.SYSTEM_MSG_ID, $"Could not create message for JSON {aJsonMsg}: {e.Message}"));
                }
                catch (JsonSerializationException e)
                {
                    res.Add(_bpLogger.LogErrorMsg(ButtplugConsts.SYSTEM_MSG_ID, $"Could not create message for JSON {aJsonMsg}: {e.Message}"));
                }
            }
            return res.ToArray();
        }

        public string Serialize(ButtplugMessage aMsg)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            var o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(aMsg)));
            var a = new JArray(o);
            _bpLogger.Trace($"Message serialized to: {a.ToString(Formatting.None)}", true);
            return a.ToString(Formatting.None);
        }

        public string Serialize(ButtplugMessage[] aMsgs)
        {
            // Warning: Any log messages in this function must be localOnly. They will possibly recurse.
            var a = new JArray();
            foreach (ButtplugMessage aMsg in aMsgs)
            {
                var o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(aMsg)));
                a.Add(o);
            }
            _bpLogger.Trace($"Message serialized to: {a.ToString(Formatting.None)}", true);
            return a.ToString(Formatting.None);
        }
    }
}