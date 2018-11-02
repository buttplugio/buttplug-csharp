using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace Buttplug.Server
{
    public class ButtplugServerOptions
    {
        [NotNull]
        public string ServerName { get; }

        [NotNull]
        public List<string> SubtypeManagerSearchPaths { get; }

        public uint MaxPingTime { get; }

        [CanBeNull]
        public DeviceManager DeviceManager { get; }

        public ButtplugServerOptions(string aServerName, uint aMaxPingTime, DeviceManager aManager = null, List<string> aPaths = null)
        {
            ServerName = aServerName;
            MaxPingTime = aMaxPingTime;

            // If someone passes in paths, assume they already know what they want to do.
            if (aPaths != null)
            {
                SubtypeManagerSearchPaths = aPaths;
            }
            else
            {
                SubtypeManagerSearchPaths = new List<string>();
                AddSubtypeManagerSearchDirectory(Directory.GetCurrentDirectory());
            }

            DeviceManager = aManager;
        }

        public void AddSubtypeManagerSearchDirectory(string aPath)
        {
            if (!Directory.Exists(aPath))
            {
                throw new ArgumentException("Path must exist on drive.");
            }

            SubtypeManagerSearchPaths.Add(aPath);
        }
    }
}
