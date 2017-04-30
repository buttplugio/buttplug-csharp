using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Buttplug
{
    public class ButtplugJSONMessageParser
    {
        Dictionary<String, Type> MessageTypes;

        public ButtplugJSONMessageParser()
        {
            var messageClasses = from t in Assembly.GetAssembly(typeof(IButtplugMessage)).GetTypes()
                                 where t.IsClass && t.Namespace == "Buttplug.Messages" && typeof(IButtplugMessage).IsAssignableFrom(t)
                                 select t;

            Console.WriteLine("Message types: " + messageClasses.Count());
            MessageTypes = new Dictionary<String, Type>();
            messageClasses.ToList().ForEach(c => {
                Console.WriteLine(c.Name);
                MessageTypes.Add(c.Name, c);
            });
        }

        public Option<IButtplugMessage> Deserialize(string aJSONMsg)
        {
            // TODO This is probably the place where the most stuff can go wrong. Test the shit out of it.... Soon. >.>
            JObject j;
            try
            {
                j = JObject.Parse(aJSONMsg);
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine("Not valid JSON!");
                Console.WriteLine(e.Data);
                return Option<IButtplugMessage>.None;
            }
            try
            {
                String msgName = j.Properties().First().Name;
                if (!MessageTypes.Keys.Contains(msgName))
                {
                    Console.WriteLine("Could not find message name!");
                    return Option<IButtplugMessage>.None;
                }
                JsonSerializer s = new JsonSerializer();
                s.MissingMemberHandling = MissingMemberHandling.Error;
                JObject r = j[msgName].Value<JObject>();
                IButtplugMessage m = (IButtplugMessage)r.ToObject(MessageTypes[msgName], s);
                return Option<IButtplugMessage>.Some(m);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not create message!");
                Console.WriteLine(e.Data);
                return Option<IButtplugMessage>.None;
            };
        }

        public static Option<string> Serialize(IButtplugMessage aMsg)
        {
            // TODO There are so very many ways this could throw
            JObject o = new JObject(new JProperty(aMsg.GetType().Name, JObject.FromObject(aMsg)));
            return o.ToString();
        }
    }
}
