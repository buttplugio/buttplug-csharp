using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ButtplugServerGUI
{
    public class ButtplugConfig
    {
        private readonly string configFile;
        private JObject config;
        private DateTime modtime;

        public ButtplugConfig(string app)
        {
            configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), app, "config.json");

            config = new JObject();
            modtime = DateTime.MinValue;
            LoadConfig();
        }

        private void LoadConfig()
        {
            if(File.Exists(configFile) && File.GetLastWriteTimeUtc(configFile) > modtime)
            {
                try
                {
                    config = JObject.Parse(File.ReadAllText(configFile));
                    modtime = File.GetLastWriteTimeUtc(configFile);
                }
                catch
                {

                }
            }
        }

        private void SaveConfig()
        {
            try
            {
                File.WriteAllText(configFile, config.ToString());
                modtime = File.GetLastWriteTimeUtc(configFile);
            }
            catch
            {

            }
        }

        public string GetValue(string key, string other = null)
        {
            var bits = key.Split('.');
            LoadConfig();
            JToken cfg = config;
            for(var i = 0; cfg != null && i < bits.Length; i++)
            {
                cfg = cfg[bits[i]];
            }
            if(cfg != null)
            {
                return cfg.Value<string>();
            }
            return other;
        }

        public void SetValue(string key, string value)
        {
            var bits = key.Split('.');
            LoadConfig();
            JToken cfg = config;
            for (var i = 0; cfg != null && i < bits.Length-1; i++)
            {
                if(cfg[bits[i]] == null)
                {
                    cfg[bits[i]] = new JObject();
                }
                cfg = cfg[bits[i]];
            }
            
            cfg[bits[bits.Length-1]] = value;
            SaveConfig();
        }
    }
}
