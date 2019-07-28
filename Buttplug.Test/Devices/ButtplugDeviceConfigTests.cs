using System;
using Buttplug.Core;
using Buttplug.Devices.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Test.Devices
{
    [TestFixture]
    class ButtplugDeviceConfigTests
    {
        [SetUp]
        public void SetUp()
        {
            DeviceConfigurationManager.Clear();
        }

        [Test]
        public void TestDeviceConfigurationManagerFailsWithoutLoad()
        {
            Action test = () => DeviceConfigurationManager.Manager.Find(new HIDProtocolConfiguration(0, 0));
            test.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void TestDeviceConfigResourceLoading()
        {
            DeviceConfigurationManager.LoadBaseConfigurationFromResource();
        }

        [Test]
        public void TestDeviceConfigMatching()
        {
            DeviceConfigurationManager.LoadBaseConfigurationFromResource();
            var factory = DeviceConfigurationManager.Manager.Find(new BluetoothLEProtocolConfiguration("LVS-Test"));
            factory.ProtocolName.Should().Be("LovenseProtocol");
        }

        [Test]
        public void TestDeviceConfigNoMatch()
        {
            DeviceConfigurationManager.LoadBaseConfigurationFromResource();
            var factory = DeviceConfigurationManager.Manager.Find(new BluetoothLEProtocolConfiguration("Whatever"));
            factory.Should().BeNull();
        }

        [Test]
        [Ignore("ET312 protocol currently turned off, we need a test protocol to use on this")]
        public void TestUserConfig()
        {
            DeviceConfigurationManager.LoadBaseConfigurationFromResource();
            var mgr = DeviceConfigurationManager.Manager;
            mgr.LoadUserConfigurationString(@"
{
   'protocols': {
            'erostek-et312': {
                'serial': {
                    'ports': [
                    'COM1',
                    '/dev/ttyUSB0'
                        ]
                }
            }
        }
    }
");
            mgr.Find(new SerialProtocolConfiguration("COM1")).Should().NotBeNull();
            mgr.Find(new SerialProtocolConfiguration("COM2")).Should().BeNull();
        }

        [Test]
        public void TestAddUserConfigWithInvalidProtocol()
        {
            DeviceConfigurationManager.LoadBaseConfigurationFromResource();
            var mgr = DeviceConfigurationManager.Manager;
            Action test = () => mgr.LoadUserConfigurationString(@"
{
   'protocols': {
            'not-a-protocol': {
                'serial': {
                    'ports': [
                    'COM1',
                    '/dev/ttyUSB0'
                        ]
                }
            }
        }
    }
");
            test.Should().Throw<ButtplugDeviceException>();
        }

        [Test]
        public void TestAddUserConfigWithConflictingBLEName()
        {
            DeviceConfigurationManager.LoadBaseConfigurationFromResource();
            var mgr = DeviceConfigurationManager.Manager;
            Action test = () => mgr.LoadUserConfigurationString(@"
{
   'protocols': {
            'mysteryvibe': {
                'btle': {
                    'names': [
                    'MV Crescendo'
                        ]
                }
            }
        }
    }
");
            test.Should().Throw<ButtplugDeviceException>();
        }

        [Test]
        public void TestAddUserConfigWithConflictingBLEService()
        {
            DeviceConfigurationManager.LoadBaseConfigurationFromResource();
            var mgr = DeviceConfigurationManager.Manager;
            Action test = () => mgr.LoadUserConfigurationString(@"
{
   'protocols': {
            'mysteryvibe': {
                'btle': {
                    'services': {
                        'f0006900-110c-478B-B74B-6F403B364A9C': {
                            'txmode': 'f0006901-110c-478B-B74B-6F403B364A9C',
                            'txvibrate': 'f0006903-110c-478B-B74B-6F403B364A9C'
                        }
                    }
                }
            }
        }
    }
");
            test.Should().Throw<ButtplugDeviceException>();
        }
    }
}
