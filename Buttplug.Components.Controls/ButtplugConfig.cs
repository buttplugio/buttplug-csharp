// <copyright file="ButtplugConfig.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace Buttplug.Components.Controls
{
    public class ButtplugConfig
    {
        private readonly string _configFile;

        [NotNull]
        private JObject _config;

        private DateTime _modtime;

        public ButtplugConfig([NotNull] string aAppName)
        {
            _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), aAppName, "config.json");

            _config = new JObject();
            _modtime = DateTime.MinValue;
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (!File.Exists(_configFile) || File.GetLastWriteTimeUtc(_configFile) <= _modtime)
            {
                return;
            }

            try
            {
                _config = JObject.Parse(File.ReadAllText(_configFile));
                _modtime = File.GetLastWriteTimeUtc(_configFile);
            }
            catch (Exception aEx)
            {
                Debug.WriteLine("Cannot load config file! " + aEx.Message);
            }
        }

        private void SaveConfig()
        {
            try
            {
                File.WriteAllText(_configFile, _config.ToString());
                _modtime = File.GetLastWriteTimeUtc(_configFile);
            }
            catch (Exception aEx)
            {
                Debug.WriteLine("Cannot save config file! " + aEx.Message);
            }
        }

        public string GetValue([NotNull] string aKey, string aOther = null)
        {
            var bits = aKey.Split('.');
            LoadConfig();
            JToken cfg = _config;
            for (var i = 0; cfg != null && i < bits.Length; i++)
            {
                cfg = cfg[bits[i]];
            }

            return cfg != null ? cfg.Value<string>() : aOther;
        }

        public void SetValue([NotNull] string aKey, [NotNull] string aValue)
        {
            var bits = aKey.Split('.');
            LoadConfig();
            JToken cfg = _config;
            for (var i = 0; cfg != null && i < bits.Length - 1; i++)
            {
                if (cfg[bits[i]] == null)
                {
                    cfg[bits[i]] = new JObject();
                }

                cfg = cfg[bits[i]];
            }

            cfg[bits[bits.Length - 1]] = aValue;
            SaveConfig();
        }
    }
}
