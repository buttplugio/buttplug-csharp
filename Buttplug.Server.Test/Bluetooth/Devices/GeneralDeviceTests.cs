using System;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using Buttplug.Server.Test.Util;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    // Each of the tests in this class is run on every bluetooth device class we know of, as listed
    // in the BluetoothSubtypeManager. Saves having to repeat the code on every device specific test,
    // at the cost of some gnarly reflection.
    //
    // Note that tests that happen here only run on one kind of device from a device info class,
    // assuming all devices will act the same. If you need varying reactions per device, put those
    // tests in the specific device test class.
    [TestFixture]
    class GeneralDeviceTests
    {
        private IBluetoothDeviceGeneralTestUtils GetInterfaceObj(IBluetoothDeviceInfo aInfo)
        {
            // Tests for IBluetoothDeviceInfo objects would normally be of the format
            //
            // BluetoothDeviceTestUtils.TestDeviceInfo<T>()
            //
            // However, since we have all of the types already listed in BluetoothSubtypeManager,
            // we can use reflection to build the dynamic type around those classes and
            // automatically test any new objects that show up in the array.
            //
            // If only the code wasn't so horribly ugly.
            var bdtu = typeof(BluetoothDeviceTestUtils<>);
            var objType = bdtu.MakeGenericType(aInfo.GetType());
            var obj = Activator.CreateInstance(objType) as IBluetoothDeviceGeneralTestUtils;
            obj.SetupTest(aInfo.Names.Length > 0 ? aInfo.Names[0] : aInfo.NamePrefixes[0]);
            return obj;
        }

        // Test general expectations for BluetoothDeviceInfo classes
        [Test]
        public void TestInfo()
        {
            TestBluetoothSubtypeManager mgr = new TestBluetoothSubtypeManager(new ButtplugLogManager());
            foreach (var devInfo in mgr.GetDefaultDeviceInfoList())
            {
                // If you fail on this, check any device types you've recently added to
                // BluetoothSubtypeManager to make sure they conform to expectations (have names or
                // nameprefixes to search on, have at least one service, etc.)
                GetInterfaceObj(devInfo).TestDeviceInfo();
            }
        }

        // Test for existence of StopDeviceCmd on Device class, and that it noops if it's the first
        // thing sent (since we have nothing to stop).
        [Test]
        public void TestStopDeviceCmd()
        {
            TestBluetoothSubtypeManager mgr = new TestBluetoothSubtypeManager(new ButtplugLogManager());
            foreach (var devInfo in mgr.GetDefaultDeviceInfoList())
            {
                GetInterfaceObj(devInfo).TestDeviceMessageNoop(new StopDeviceCmd(1));
            }
        }
    }
}
