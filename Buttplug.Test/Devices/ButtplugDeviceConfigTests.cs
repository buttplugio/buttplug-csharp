using Buttplug.Devices.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Test.Devices
{
    [TestFixture]
    class ButtplugDeviceConfigTests
    {
        [Test]
        public void TestDeviceConfigurationManagerFailsWithoutLoad()
        {
            //DeviceConfigurationManager.Manager.Invoking((x) => x.FindProtocol(new NullProtocolConfiguration())).Should()
            //   .Throw<NullReferenceException>();
        }

        [Test]
        public void TestDeviceConfigResourceLoading()
        {
            DeviceConfigurationManager.LoadBaseConfigurationFromResource();
        }

        [Test]
        [Ignore("Need to rewrite configuration format to include protocol names")]
        public void TestDeviceConfigMatching()
        {
            DeviceConfigurationManager.LoadBaseConfigurationFromResource();
            var factory = DeviceConfigurationManager.Manager.Find(new BluetoothLEProtocolConfiguration("LVS-Test"));
            factory.Config.Should().Be("LovenseProtocol");
        }

        [Test]
        public void TestDeviceConfigNoMatch()
        {
            DeviceConfigurationManager.LoadBaseConfigurationFromResource();
            var factory = DeviceConfigurationManager.Manager.Find(new BluetoothLEProtocolConfiguration("Whatever"));
            factory.Should().BeNull();
        }
    }
}
