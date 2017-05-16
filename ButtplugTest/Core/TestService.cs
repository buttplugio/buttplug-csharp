using System.Collections.Generic;
using Buttplug.Core;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace ButtplugTest.Core
{
    internal class TestService : ButtplugService
    {
        public TestService()
        {
            DebuggerTarget t = new DebuggerTarget();
            LogManager.Configuration = new LoggingConfiguration();
            LogManager.Configuration.AddTarget("debugger", t);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, t));
            LogManager.Configuration = LogManager.Configuration;
        }

        public TestService(TestDeviceManager mgr)
        {
            AddManager(mgr);
        }
        
        public Dictionary<uint, ButtplugDevice> GetDevices()
        {
            return new Dictionary<uint, ButtplugDevice>();
            //return _devices;
        }
    }
}