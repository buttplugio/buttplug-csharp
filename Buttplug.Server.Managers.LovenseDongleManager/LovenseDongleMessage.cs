using Newtonsoft.Json;

namespace Buttplug.Server.Managers.LovenseDongleManager
{
    class LovenseDongleOutgoingMessage
    {
        public class MessageType
        {
            public static readonly string USB = "usb";
            public static readonly string Toy = "toy";
        }

        public class MessageFunc
        {
            public static readonly string Search = "search";
            public static readonly string StopSearch = "stopSearch";
            public static readonly string Status = "statuss";
            public static readonly string Command = "command";
            public static readonly string ToyData = "toyData";
        }

        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public string Type;

        [JsonProperty(Required = Required.Always, PropertyName = "func")]
        public string Func;

        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id;

        [JsonProperty(PropertyName = "cmd", NullValueHandling = NullValueHandling.Ignore)]
        public string Command;

        public LovenseDongleOutgoingMessage()
        {
        }
    }

    class LovenseDongleIncomingMessage : LovenseDongleOutgoingMessage
    {
        [JsonProperty(PropertyName = "result", NullValueHandling = NullValueHandling.Ignore)]
        public int Result;

        [JsonProperty(PropertyName = "data.data", NullValueHandling = NullValueHandling.Ignore)]
        public string Data;

        public LovenseDongleIncomingMessage()
        {
        }
    }
}
