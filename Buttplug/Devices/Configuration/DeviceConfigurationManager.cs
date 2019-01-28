using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Buttplug.Devices.Protocols;

namespace Buttplug.Devices.Configuration
{
    class DeviceConfigurationManager
    {
        private static DeviceConfigurationManager _manager;

        public static DeviceConfigurationManager Manager
        {
            get
            {
                if (_manager == null)
                {
                    throw new NullReferenceException("Must load manager from file or resource before using!");
                }

                return _manager;
            }
        }

        private readonly Dictionary<string, IProtocolConfiguration> _protocolConfigs = new Dictionary<string, IProtocolConfiguration>();
        private readonly Dictionary<string, Type> _protocolDict = new Dictionary<string, Type>();
        private readonly List<IProtocolConfiguration> _whiteList = new List<IProtocolConfiguration>();
        private readonly List<IProtocolConfiguration> _blackList = new List<IProtocolConfiguration>();

        protected DeviceConfigurationManager()
        {
            AddProtocol("lovense", typeof(LovenseProtocol));
            AddProtocol("xinput", typeof(XInputProtocol));
        }

        public static void LoadFromFile(string aFileName)
        {
            DeviceConfigurationManager._manager = new DeviceConfigurationManager();
        }

        public void AddProtocol(string aProtocolName, Type aProtocolType)
        {
            _protocolDict.Add(aProtocolName, aProtocolType);
        }

        public void AddProtocolConfig(string aProtocolName, IProtocolConfiguration aConfiguration)
        {
            _protocolConfigs.Add(aProtocolName, aConfiguration);
        }

        public void AddWhitelist(IProtocolConfiguration aConfiguration)
        {
            _whiteList.Add(aConfiguration);
        }

        public void AddBlacklist(IProtocolConfiguration aConfiguration)
        {
            _blackList.Add(aConfiguration);
        }

        public Type FindProtocol(IProtocolConfiguration aConfig)
        {
            if (_whiteList.Any())
            {
                var found = false;
                foreach (var config in _whiteList)
                {
                    if (!aConfig.Matches(config))
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    return null;
                }
            }

            if (_blackList.Any())
            {
                foreach (var config in _blackList)
                {
                    if (aConfig.Matches(config))
                    {
                        return null;
                    }
                }
            }

            foreach (var config in _protocolConfigs)
            {
                if (!config.Value.Matches(aConfig))
                {
                    continue;
                }

                if (!_protocolDict.ContainsKey(config.Key))
                {
                    // Todo This means we found a device we have config but no protocol for. We should log here and return null.
                    return null;
                }

                return _protocolDict[config.Key];
            }

            return null;
        }
    }
}
