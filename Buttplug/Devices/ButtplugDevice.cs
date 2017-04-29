using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth;
using LanguageExt;
using ButtplugMessages;

namespace Buttplug
{
    public interface IButtplugDevice
    {
        String Name { get; }
        UInt32 DeviceIndex { get; }
        bool ParseMessage(IButtplugDeviceMessage aMsg);
        bool Connect();
        bool Disconnect();
    }

    public abstract class ButtplugBluetoothDeviceFactory
    {
        protected List<String> NameFilters { get; }
        protected List<Guid> ServiceFilters { get; }
        public ButtplugBluetoothDeviceFactory()
        {
            NameFilters = new List<String>();
            ServiceFilters = new List<Guid>();
        }
        public bool MayBeDevice(BluetoothLEAdvertisement aAdvertisement)
        {
            if (!NameFilters.Any() && !ServiceFilters.Any())
            {
                return false;
            }
            if (NameFilters.Any() && !NameFilters.Contains(aAdvertisement.LocalName))
            {
                return false;
            }
            if (ServiceFilters.Any() &&
                !ServiceFilters.Union(aAdvertisement.ServiceUuids).Any())
            {
                return false;
            }
            return true;
        }
        abstract public Task<Option<IButtplugDevice>> CreateDeviceAsync(BluetoothLEDevice aDevice);
    }
}
