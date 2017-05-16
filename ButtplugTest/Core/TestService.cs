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
            // Build ourselves an NLog manager just so we can see what's going on.
            DebuggerTarget t = new DebuggerTarget();
            LogManager.Configuration = new LoggingConfiguration();
            LogManager.Configuration.AddTarget("debugger", t);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, t));
            LogManager.Configuration = LogManager.Configuration;
        }

        public TestService(TestDeviceSubtypeManager mgr) : this()
        {
            GetDeviceManager().AddManager(mgr);
        }
    }
}