using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Buttplug.Messages;
using NLog;
using Xunit;

namespace Buttplug
{
    public class ButtplugJsonMessageParser
    {
        Dictionary<String, Type> MessageTypes;
        Logger BPLogger;
        public ButtplugJsonMessageParser()
        {
            BPLogger = LogManager.GetLogger("Buttplug");
            BPLogger.Debug($"Setting up {this.GetType().Name}");
            var messageClasses = from t in Assembly.GetAssembly(typeof(IButtplugMessage)).GetTypes()
                                 where t.IsClass && t.Namespace == "Buttplug.Messages" && typeof(IButtplugMessage).IsAssignableFrom(t)
                                 select t;

            BPLogger.Debug($"Message type count: {messageClasses.Count()}");
            MessageTypes = new Dictionary<String, Type>();
            messageClasses.ToList().ForEach(c => {
                BPLogger.Debug($"- {c.Name}");
                MessageTypes.Add(c.Name, c);
            });
        }

        public Option<IButtplugMessage> Deserialize(string aJSONMsg)
        {
            BPLogger.Trace($"Got JSON Message: {aJSONMsg}");
            // TODO This is probably the place where the most stuff can go wrong. Test the shit out of it.... Soon. >.>
            JObject j;
            try
            {
                j = JObject.Parse(aJSONMsg);
            }
            catch (JsonReaderException e)
            {
                BPLogger.Warn($"Not valid JSON: {aJSONMsg}");
                BPLogger.Warn(e.Message);
                return Option<IButtplugMessage>.None;
            }
            try
            {
                String msgName = j.Properties().First().Name;
                if (!MessageTypes.Keys.Contains(msgName))
                {
                    BPLogger.Warn($"{msgName} is not a valid message class");
                    return Option<IButtplugMessage>.None;
                }
                JsonSerializer s = new JsonSerializer();
                s.MissingMemberHandling = MissingMemberHandling.Error;
                JObject r = j[msgName].Value<JObject>();
                IButtplugMessage m = (IButtplugMessage)r.ToObject(MessageTypes[msgName], s);
                BPLogger.Trace($"Message successfully parsed as {msgName} type");
                return Option<IButtplugMessage>.Some(m);
            }
            catch (Exception e)
            {
                BPLogger.Warn($"Could not create message for JSON {aJSONMsg}");
                BPLogger.Warn(e.Message);
                return Option<IButtplugMessage>.None;
            };
        }

        public static Option<string> Serialize(IButtplugMessage aMsg)
        {
            // TODO There are so very many ways this could throw
            JObject o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(aMsg)));
            return o.ToString(Formatting.None);
        }
        
        #region xUnit Tests

        [Fact]
        public void JsonConversionTest()
        {
            TestMessage m = new TestMessage("ThisIsATest");
            var msg = Serialize(m);
            Assert.True(msg.IsSome);
            msg.IfSome((x) => Assert.Equal(x, "{\"TestMessage\":{\"TestString\":\"ThisIsATest\"}}"));
        }
        #endregion
    }
}
