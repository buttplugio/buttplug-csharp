using System;
using Newtonsoft.Json;

namespace Buttplug.Server.Managers.LovenseDongleManager
{

    public enum LovenseDongleResultCode
    {
        CommandSuccess = 200,
        DeviceConnectFailed = 201,
        DeviceConnectSuccess = 202,
        SearchStarted = 205,
        SearchStopped = 206,
        DeviceDisconnected = 403,
        DongleScanningInterruption = 501,
    }

    public class LovenseDongleMessageType
    {
        public const string USB = "usb";
        public const string Toy = "toy";
    }

    public class LovenseDongleMessageFunc
    {
        public const string Search = "search";
        public const string StopSearch = "stopSearch";
        public const string IncomingStatus = "status";
        public const string Command = "command";
        public const string ToyData = "toyData";
        public const string Connect = "connect";
    }

    public class LovenseDongleOutgoingMessage
    {
        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public string Type;

        [JsonProperty(Required = Required.Always, PropertyName = "func")]
        public string Func;

        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id;

        [JsonProperty(PropertyName = "cmd", NullValueHandling = NullValueHandling.Ignore)]
        public string Command;

        [JsonProperty(PropertyName = "eager", NullValueHandling = NullValueHandling.Ignore)]
        public uint? Eager;

        public LovenseDongleOutgoingMessage()
        {
        }
    }

    class LovenseDongleIncomingData
    {
        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id;

        [JsonProperty(PropertyName = "data", NullValueHandling = NullValueHandling.Ignore)]
        public string Data;

        [JsonProperty(PropertyName = "status", NullValueHandling = NullValueHandling.Ignore)]
        public uint? Status;
    }

    class LovenseDongleIncomingMessage : LovenseDongleOutgoingMessage
    {
        [JsonProperty(PropertyName = "result", NullValueHandling = NullValueHandling.Ignore)]
        public int Result;

        [JsonProperty(PropertyName = "data", NullValueHandling = NullValueHandling.Ignore)]
        public LovenseDongleIncomingData Data;

        [JsonProperty(PropertyName = "message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message;

        public LovenseDongleIncomingMessage()
        {
        }
    }
}
