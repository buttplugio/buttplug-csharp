using Buttplug.Core;
using HidLibrary;

namespace Buttplug.Server.Managers.HidManager
{
    public interface IHidDeviceInfo
    {
        string Name { get; }

        int ProductId { get; }

        int VendorId { get; }

        IButtplugDevice CreateDevice(IButtplugLogManager buttplugLogManager, IHidDevice aHid);
    }
}