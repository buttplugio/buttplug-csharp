using System;
using System.Collections.Generic;
using System.Linq;

namespace Buttplug.Devices.Configuration
{
    public class BluetoothLEProtocolConfiguration : IProtocolConfiguration
    {
        public readonly List<string> Names;
        public readonly List<Guid> Services;

        /// <summary>
        /// Dictionary of service to characteristic name/Guid dictionary.
        /// </summary>
        public readonly Dictionary<Guid, Dictionary<string, Guid>> Characteristics;

        public BluetoothLEProtocolConfiguration(IEnumerable<string> aNames,
            IEnumerable<Guid> aServices = null,
            Dictionary<Guid, Dictionary<string, Guid>> aCharacteristics = null)
        {
            Names = aNames.ToList();

            Services = aServices != null ? aServices.ToList() : new List<Guid>();

            // TODO Fail on similarly named characteristics

            // TODO Fail on devices with multiple services without characteristic lists
            Characteristics = aCharacteristics ?? new Dictionary<Guid, Dictionary<string, Guid>>();
        }

        internal BluetoothLEProtocolConfiguration(BluetoothLEIdentifier aId, Dictionary<Guid, Dictionary<string, Guid>> aConfig)
            : this(aId.Names, aId.Services, aConfig)
        {
        }

        public BluetoothLEProtocolConfiguration(string aName)
            : this(new[] { aName })
        {
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

            // TODO Put in advertised service checking, but that hasn't really been needed so far.
            return false;
        }
    }
}
