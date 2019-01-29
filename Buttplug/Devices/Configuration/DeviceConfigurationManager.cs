using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Buttplug.Core;
using Buttplug.Devices.Protocols;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        private readonly Dictionary<string, Type> _protocolTypes = new Dictionary<string, Type>();
        private readonly List<IProtocolConfiguration> _whiteList = new List<IProtocolConfiguration>();
        private readonly List<IProtocolConfiguration> _blackList = new List<IProtocolConfiguration>();

        protected DeviceConfigurationManager()
        {
            AddProtocol("lovense", typeof(LovenseProtocol));
            AddProtocol("xinput", typeof(XInputProtocol));
        }

        protected void LoadBaseConfigurationFromResourceInternal()
        {
            var deviceConfig = ButtplugUtils.GetStringFromFileResource("Buttplug.buttplug-device-config.yml");
            var input = new StringReader(deviceConfig);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new HyphenatedNamingConvention())
                .Build();

            var configObject = deserializer.Deserialize<DeviceConfigurationFile>(input);
            foreach (var protocolInfo in configObject.Protocols)
            {
                var protocolName = protocolInfo.Key;
                var protocolId = protocolInfo.Value?.Identifier;
                var protocolConfig = protocolInfo.Value?.Configuration;
                if (protocolId?.Btle != null)
                {
                    var btleProtocolConfig = new BluetoothLEProtocolConfiguration(protocolId.Btle, protocolConfig?.Btle);
                    AddProtocolConfig(protocolName, btleProtocolConfig);
                }

                if (protocolId?.Hid != null)
                {
                    var hidProtocolConfig = new HIDProtocolConfiguration(protocolId.Hid, protocolConfig?.Hid);
                    AddProtocolConfig(protocolName, hidProtocolConfig);
                }

                if (protocolId?.Serial != null)
                {
                }

                if (protocolId?.Usb != null)
                {
                    var usbProtocolConfig = new USBProtocolConfiguration(protocolId.Usb, protocolConfig?.Usb);
                    AddProtocolConfig(protocolName, usbProtocolConfig);
                }
            }
        }

        /// <summary>
        /// Loads configuration file from the configuration packed with the library on compilation.
        /// </summary>
        public static void LoadBaseConfigurationFromResource()
        {
            _manager = new DeviceConfigurationManager();
            _manager.LoadBaseConfigurationFromResourceInternal();
        }

        public static void LoadBaseConfigurationFile(string aFileName)
        {
            _manager = new DeviceConfigurationManager();
        }

        /// <summary>
        /// Loads user configuration. We require a base configuration to be loaded first, as user
        /// configurations should only add on to that.
        /// </summary>
        /// <param name="aFileName">Path to the user configuration file.</param>
        public void LoadUserConfigurationFile(string aFileName)
        {
        }

        public void AddProtocol(string aProtocolName, Type aProtocolType)
        {
            _protocolTypes.Add(aProtocolName, aProtocolType);
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

                if (!_protocolTypes.ContainsKey(config.Key))
                {
                    // Todo This means we found a device we have config but no protocol for. We should log here and return null.
                    return null;
                }

                return _protocolTypes[config.Key];
            }

            return null;
        }
    }
}
