using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buttplug.Core.Messages;
using Newtonsoft.Json;

namespace Buttplug.Devices.Configuration
{
    public class DeviceConfiguration
    {
        [JsonProperty("identifier")] public List<string> Identifiers;

        [JsonProperty("name")] public Dictionary<string, string> Names;

        [JsonProperty("messages", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, MessageAttributes> Attributes = new Dictionary<string, MessageAttributes>();

        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(IDictionary<TKey, TValue> dictA, IDictionary<TKey, TValue> dictB)
            where TValue : class
        {
            return dictA.Keys.Union(dictB.Keys).ToDictionary(k => k, k => dictA.ContainsKey(k) ? dictA[k] : dictB[k]);
        }

        public void AddDefaults(Dictionary<string, MessageAttributes> aDefaults)
        {
            // Merge dictionaries. If a key already exists in our configuration,
            // ignore the version in the defaults.
            Attributes = Merge(Attributes, aDefaults);
        }
    }

    public abstract class ProtocolConfiguration : IProtocolConfiguration
    {
        private List<DeviceConfiguration> _deviceConfig;

        public List<DeviceConfiguration> DeviceConfigs => _deviceConfig;

        public void SetDeviceConfig(List<DeviceConfiguration> aDeviceConfig)
        {
            _deviceConfig = aDeviceConfig;
        }

        public DeviceConfiguration GetDeviceConfig(string aIdentifier)
        {
            foreach (var conf in _deviceConfig)
            {
                if (conf.Identifiers.Contains(aIdentifier))
                {
                    return conf;
                }
            }

            return null;
        }

        public abstract bool Matches(IProtocolConfiguration aConfig);

        public abstract void Merge(IProtocolConfiguration aConfig);
    }
}