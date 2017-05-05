using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Buttplug.Messages;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Xunit;

namespace Buttplug.Core
{
    public class ButtplugJsonMessageParser
    {
        private readonly Dictionary<String, Type> _messageTypes;
        private readonly Logger _bpLogger;
        public ButtplugJsonMessageParser()
        {
            _bpLogger = LogManager.GetLogger("Buttplug");
            _bpLogger.Debug($"Setting up {GetType().Name}");
            IEnumerable<Type> allTypes;
            try
            {
                allTypes = Assembly.GetAssembly(typeof(IButtplugMessage)).GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                allTypes = e.Types;
            }
            IEnumerable<Type> messageClasses = from t in allTypes
                                               where t != null && t.IsClass && t.Namespace == "Buttplug.Messages" && typeof(IButtplugMessage).IsAssignableFrom(t)
                                               select t;

            var enumerable = messageClasses as Type[] ?? messageClasses.ToArray();
            _bpLogger.Debug($"Message type count: {enumerable.Count()}");
            _messageTypes = new Dictionary<String, Type>();
            enumerable.ToList().ForEach(c => {
                _bpLogger.Debug($"- {c.Name}");
                _messageTypes.Add(c.Name, c);
            });
        }

        public Option<IButtplugMessage> Deserialize(string aJsonMsg)
        {
            _bpLogger.Trace($"Got JSON Message: {aJsonMsg}");
            // TODO This is probably the place where the most stuff can go wrong. Test the shit out of it.... Soon. >.>
            JObject j;
            try
            {
                j = JObject.Parse(aJsonMsg);
            }
            catch (JsonReaderException e)
            {
                _bpLogger.Warn($"Not valid JSON: {aJsonMsg}");
                _bpLogger.Warn(e.Message);
                return Option<IButtplugMessage>.None;
            }
            try
            {
                var msgName = j.Properties().First().Name;
                if (!_messageTypes.Keys.Contains(msgName))
                {
                    _bpLogger.Warn($"{msgName} is not a valid message class");
                    return Option<IButtplugMessage>.None;
                }
                var s = new JsonSerializer {MissingMemberHandling = MissingMemberHandling.Error};
                var r = j[msgName].Value<JObject>();
                var m = (IButtplugMessage)r.ToObject(_messageTypes[msgName], s);
                _bpLogger.Trace($"Message successfully parsed as {msgName} type");
                return Option<IButtplugMessage>.Some(m);
            }
            catch (Exception e)
            {
                _bpLogger.Warn($"Could not create message for JSON {aJsonMsg}");
                _bpLogger.Warn(e.Message);
                return Option<IButtplugMessage>.None;
            };
        }

        public static Option<string> Serialize(IButtplugMessage aMsg)
        {
            // TODO There are so very many ways this could throw
            var o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(aMsg)));
            return o.ToString(Formatting.None);
        }
        
        #region xUnit Tests

        [Fact]
        public void JsonConversionTest()
        {
            var m = new TestMessage("ThisIsATest");
            var msg = Serialize(m);
            Assert.True(msg.IsSome);
            msg.IfSome((x) => Assert.Equal(x, "{\"TestMessage\":{\"TestString\":\"ThisIsATest\"}}"));
        }
        #endregion
    }
}
