using System;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage;
using Buttplug.Devices.Configuration;

namespace Buttplug.Server.Managers.UwpHidManager
{
    public class UwpHidManager : DeviceSubtypeManager
    {
        public static Guid HID_INTERFACE_CLASS_GUID = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        public UwpHidManager(IButtplugLogManager aLogManager) : base(aLogManager)
        {
        }

        protected async Task StartScanningInternal()
        {
            var factories = DeviceConfigurationManager.Manager.GetAllOfType<HIDProtocolConfiguration>();

            var selectorPredicates = string.Empty;

            foreach (var factory in factories)
            {
                if (selectorPredicates != string.Empty)
                {
                    selectorPredicates += " OR ";
                }
                var config = factory.Config as HIDProtocolConfiguration;
                selectorPredicates += $"(System.DeviceInterface.Hid.VendorId:={config.VendorId}"
                    + $" AND System.DeviceInterface.Hid.ProductId:={config.ProductId})";
            }

            // UWP doesn't have a 
            var selector =
                "System.Devices.InterfaceClassGuid:={" + HID_INTERFACE_CLASS_GUID + "}"
                + " AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True"
                + $" AND {selectorPredicates}";
            
            var deviceList = await DeviceInformation.FindAllAsync(selector);
            foreach (var device in deviceList)
            {
                var hidDevice =
                    await HidDevice.FromIdAsync(device.Id,
                        FileAccessMode.ReadWrite);
                var vid = hidDevice.VendorId;
                var pid = hidDevice.ProductId;
                var finder = new HIDProtocolConfiguration(vid, pid);
                var factory = DeviceConfigurationManager.Manager.Find(finder);
                if (factory == null)
                {
                    // todo Error here
                    continue;
                }
                var bpDevice = await factory.CreateDevice(LogManager, new UwpHidDeviceInterface(LogManager, hidDevice)).ConfigureAwait(false);
                InvokeDeviceAdded(new DeviceAddedEventArgs(bpDevice));
            }
        }

        public override void StartScanning()
        {
            StartScanningInternal().Wait();
        }

        public override void StopScanning()
        {
            //throw new NotImplementedException();
        }

        public override bool IsScanning()
        {
            return false;
            //throw new NotImplementedException();
        }
    }
}
