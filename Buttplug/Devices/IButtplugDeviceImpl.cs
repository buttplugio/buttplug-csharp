using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;

namespace Buttplug.Devices
{
    public interface IButtplugDeviceImpl
    {
        event EventHandler<ButtplugDeviceDataEventArgs> DataReceived;

        event EventHandler DeviceRemoved;

        string Name { get; }

        string Address { get; }

        bool Connected { get; }

        void Disconnect();

        Task WriteValueAsync(byte[] aValue, CancellationToken aToken);

        Task WriteValueAsync(string aEndpointName, byte[] aValue, CancellationToken aToken);

        Task WriteValueAsync(byte[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task WriteValueAsync(string aEndpointName, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<byte[]> ReadValueAsync(CancellationToken aToken);

        Task<byte[]> ReadValueAsync(string aEndpointName, CancellationToken aToken);

        Task SubscribeToUpdatesAsync();

        Task SubscribeToUpdatesAsync(string aEndpointName);
    }
}
