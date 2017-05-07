using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LanguageExt;
using static LanguageExt.Prelude;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Buttplug.Core
{
    public class ButtplugJsonMessageParser
    {
        private readonly Dictionary<string, Type> _messageTypes;
        private readonly Logger _bpLogger;
        public ButtplugJsonMessageParser()
        {
            _bpLogger = LogManager.GetLogger(GetType().FullName);
            _bpLogger.Debug($"Setting up {GetType().Name}");
            IEnumerable<Type> allTypes;
            // Some classes in the library may not load on certain platforms due to missing symbols.
            // If this is the case, we should still find messages even though an exception was thrown.
            try
            {
                allTypes = Assembly.GetAssembly(typeof(IButtplugMessage)).GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                allTypes = e.Types;
            }
            var messageClasses = from t in allTypes
                                 where t != null && t.IsClass && t.Namespace == "Buttplug.Messages" && typeof(IButtplugMessage).IsAssignableFrom(t)
                                 select t;

            var enumerable = messageClasses as Type[] ?? messageClasses.ToArray();
            _bpLogger.Debug($"Message type count: {enumerable.Count()}");
            _messageTypes = new Dictionary<string, Type>();
            enumerable.ToList().ForEach(c => {
                _bpLogger.Debug($"- {c.Name}");
                _messageTypes.Add(c.Name, c);
            });
        }

        public Either<string, IButtplugMessage> Deserialize(string aJsonMsg)
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
            try
            {
                var msgName = j.Properties().First().Name;
                if (!_messageTypes.Keys.Contains(msgName))
                {
                    return $"{msgName} is not a valid message class";
                }
                // TODO These variable names? Really? Does this look like math code?
                var s = new JsonSerializer {MissingMemberHandling = MissingMemberHandling.Error};
                var r = j[msgName].Value<JObject>();
                var m = (IButtplugMessage)r.ToObject(_messageTypes[msgName], s);
                var validMsg = m.Check();
                if (validMsg.IsSome)
                {
                    string err = null;
                    validMsg.IfSome(x => err = x);
                    return err;
                }
                _bpLogger.Trace($"Message successfully parsed as {msgName} type");
                // Can't get Either<> to coerce m into a IButtplugMessage so we're having to pull in
                // the internal type. I guess the cast doesn't resolve to a discernable type?
                return Right<string, IButtplugMessage>(m);
            }
            catch (Exception e)
            {
                return $"Could not create message for JSON {aJsonMsg}: {e.Message}";
            };
        }

        public static Option<string> Serialize(IButtplugMessage aMsg)
        {
            if (aMsg.Check().IsSome)
            {
                return new OptionNone();
            }
            // TODO There are so very many ways this could throw
            var o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(aMsg)));
            return o.ToString(Formatting.None);
        }
    }
}
