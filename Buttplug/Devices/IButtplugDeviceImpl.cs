using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Buttplug.Devices
{
    public interface IButtplugDeviceImpl
    {
        event EventHandler<ButtplugDeviceDataEventArgs> DataReceived;

        event EventHandler DeviceRemoved;

        string Name { get; }

        string Address { get; }

        bool Connected { get; }

        IEnumerable<string> DeviceEndpoints { get; }

        void Disconnect();

        // Classes take dictionary style objects for options, since we end up with way too many
        // signatures otherwise. This means we can handle differences in options between
        // communication types easily.

        Task WriteValueAsync(byte[] aValue, CancellationToken aToken = default(CancellationToken));

        Task WriteValueAsync(byte[] aValue, ButtplugDeviceWriteOptions aOptions = default(ButtplugDeviceWriteOptions), CancellationToken aToken = default(CancellationToken));

        Task<byte[]> ReadValueAsync(CancellationToken aToken = default(CancellationToken));

        Task<byte[]> ReadValueAsync(ButtplugDeviceReadOptions aOptions = default(ButtplugDeviceReadOptions), CancellationToken aToken = default(CancellationToken));

        Task SubscribeToUpdatesAsync(ButtplugDeviceReadOptions aOptions = default(ButtplugDeviceReadOptions));
    }
}
