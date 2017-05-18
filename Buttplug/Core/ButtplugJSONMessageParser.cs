using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Buttplug.Logging;
using Buttplug.Messages;
using static LanguageExt.Prelude;

namespace Buttplug.Core
{
    internal class ButtplugJsonMessageParser
    {
        private readonly Dictionary<string, Type> _messageTypes;
        private readonly IButtplugLog _bpLogger;

        public ButtplugJsonMessageParser(IButtplugLogManager aLogManager)
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
        }

        public Either<string, ButtplugMessage> Deserialize(string aJsonMsg)
        {
            _bpLogger.Trace($"Got JSON Message: {aJsonMsg}");
            JObject j;
            try
            {
                j = JObject.Parse(aJsonMsg);
            }
            catch (JsonReaderException e)
            {
                _bpLogger.Debug($"Not valid JSON: {aJsonMsg}");
                _bpLogger.Debug(e.Message);
                return "Not valid JSON";
            }
            if (!j.Properties().Any())
            {
                return "No message name available";
            }
            var msgName = j.Properties().First().Name;
            if (!_messageTypes.Keys.Any() || !_messageTypes.Keys.Contains(msgName))
            {
                return $"{msgName} is not a valid message class";
            }
            var s = new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Error };
            ButtplugMessage m;
            // This specifically could fail due to object conversion.
            try
            {
                var r = j[msgName].Value<JObject>();
                m = (ButtplugMessage)r.ToObject(_messageTypes[msgName], s);
            }
            catch (InvalidCastException e)
            {
                return $"Could not create message for JSON {aJsonMsg}: {e.Message}";
            }
            catch (JsonSerializationException e)
            {
                return $"Could not create message for JSON {aJsonMsg}: {e.Message}";
            }
            _bpLogger.Trace($"Message successfully parsed as {msgName} type");
            // Can't get Either<> to coerce m into a IButtplugMessage so we're having to pull in
            // the internal type. I guess the cast doesn't resolve to a discernable type?
            return Right<string, ButtplugMessage>(m);
        }

        public static string Serialize(ButtplugMessage aMsg)
        {
            var o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(aMsg)));
            return o.ToString(Formatting.None);
        }
    }
}