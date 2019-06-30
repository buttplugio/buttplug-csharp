using System;
using System.Collections.Generic;
using System.Linq;
using Buttplug.Core;

namespace Buttplug.Devices.Configuration
{
    public class BluetoothLEProtocolConfiguration : IProtocolConfiguration
    {
        public List<string> Names = new List<string>();
        public Dictionary<Guid, Dictionary<string, Guid>> Services;

        public BluetoothLEProtocolConfiguration(IEnumerable<string> aNames,
            Dictionary<Guid, Dictionary<string, Guid>> aServices = null)
        {
            Names = aNames?.ToList() ?? Names;

            Services = aServices ?? new Dictionary<Guid, Dictionary<string, Guid>>();

            // TODO Fail on similarly named characteristics in same service.
        }

        internal BluetoothLEProtocolConfiguration(BluetoothLEInfo aInfo)
            : this(aInfo.Names, aInfo.Services)
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
                    if (!string.IsNullOrEmpty(otherName) && otherName.StartsWith(tempName))
                    {
                        return true;
                    }
                }
            }

            // TODO Put in advertised service checking, but that hasn't really been needed so far.
            return false;
        }

        public void Merge(IProtocolConfiguration aConfig)
        {
            if (!(aConfig is BluetoothLEProtocolConfiguration bleConfig))
            {
                throw new ArgumentException();
            }

            if (bleConfig.Names != null)
            {
                var overlap = bleConfig.Names.Intersect(Names);
                if (overlap.Any())
                {
                    throw new ButtplugDeviceException($"User and Device configs have repeated BLE names: {overlap}");
                }

                Names = Names.Union(bleConfig.Names).ToList();
            }

            if (bleConfig.Services != null)
            {
                foreach (var service in bleConfig.Services)
                {
                    if (Services.ContainsKey(service.Key))
                    {
                        throw new ButtplugDeviceException($"User and Device configs have repeated BLE services: {service.Key}");
                    }

                    Services.Add(service.Key, service.Value);
                }
            }
        }
    }
}
