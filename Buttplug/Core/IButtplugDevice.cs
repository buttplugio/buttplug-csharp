using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth;
using LanguageExt;
using Buttplug.Messages;

namespace Buttplug
{
    public interface IButtplugDevice
    {
        String Name { get; }
        bool ParseMessage(IButtplugDeviceMessage aMsg);
    }

}
