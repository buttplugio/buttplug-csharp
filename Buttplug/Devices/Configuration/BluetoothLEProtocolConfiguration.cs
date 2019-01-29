using System;
using System.Collections.Generic;
using System.Linq;

namespace Buttplug.Devices.Configuration
{
    public class BluetoothLEProtocolConfiguration : IProtocolConfiguration
    {
        public readonly List<string> Names = new List<string>();
        public readonly List<Guid> Services = new List<Guid>();
        /// <summary>
        /// Dictionary of service to characteristic name/Guid dictionary.
        /// </summary>
        public readonly Dictionary<Guid, Dictionary<string, Guid>> Characteristics = new Dictionary<Guid, Dictionary<string, Guid>>();

        internal BluetoothLEProtocolConfiguration(BluetoothLEIdentifier aId, Dictionary<Guid, Dictionary<string, Guid>> aConfig)
        {
            Names = aId.Names;
            Services = aId.Services;
            if (aConfig == null)
            {
                return;
            }
            foreach (var serviceInfo in aConfig)
            {
                Characteristics.Add(serviceInfo.Key, serviceInfo.Value);
            }
        }

        public BluetoothLEProtocolConfiguration(string aName)
        {
            Names.Add(aName);
        }

        public BluetoothLEProtocolConfiguration(IEnumerable<string> aNames,
            IEnumerable<Guid> aServices = null,
            Dictionary<Guid, Dictionary<string, Guid>> aCharacteristics = null)
        {
            Names = aNames.ToList();
            Services = aServices.ToList();
            Characteristics = aCharacteristics;
        }

        public bool Matches(IProtocolConfiguration aConfig)
        {
            if (!(aConfig is BluetoothLEProtocolConfiguration btleConfig))
            {
                return false;
            }

            // Right now we only support asterisk as a final character, and treat this as a "starts
            // with" check.
            foreach (var name in Names)
            {
                if (btleConfig.Names.Contains(name))
                {
                    return true;
                }

                if (!name.EndsWith("*"))
                {
                    continue;
                }

                var tempName = name.Substring(0, name.Length - 1);
                foreach (var otherName in btleConfig.Names)
                {
                    if (otherName.StartsWith(tempName))
                    {
                        return true;
                    }
                }
            }

            // TODO Put in service checking, but that hasn't really been needed so far.
            return false;
        }
    }
}
