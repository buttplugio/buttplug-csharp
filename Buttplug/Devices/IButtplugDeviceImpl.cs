using System;
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

        void Disconnect();

        // Not really the way I wanted to do this, but here we are. This interface needs to cover all
        // possible communication types, including:
        //
        // - Bluetooth, which may have multiple characteristics we need to talk to
        // - USB, which may have multiple endpoints we need to talk to
        // - Serial, which is just rx/tx but we'll need expected lengths as writes aren't atomic in
        // some cases
        // - HID, which is just rx/tx.
        //
        // So, we end up with a ton of weird overloads. I was hoping to divide this up into multiple
        // interfaces and implement some sort of trait system, but C# wasn't real willing to help on
        // that end, so this is what we get. All of these are abstract in ButtplugDeviceImpl, so they
        // must be defined or throw NotImplementedException in each DeviceImpl final class. This
        // makes it clearer where things have gone wrong when we hit an unexpected connection type.

        Task WriteValueAsync(byte[] aValue, CancellationToken aToken);

        Task WriteValueAsync(string aEndpointName, byte[] aValue, CancellationToken aToken);

        Task WriteValueAsync(byte[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task WriteValueAsync(string aEndpointName, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<byte[]> ReadValueAsync(CancellationToken aToken);

        Task<byte[]> ReadValueAsync(string aEndpointName, CancellationToken aToken);

        Task<byte[]> ReadValueAsync(uint aLength, CancellationToken aToken);

        Task<byte[]> ReadValueAsync(string aEndpointName, uint aLength, CancellationToken aToken);

        Task SubscribeToUpdatesAsync();

        Task SubscribeToUpdatesAsync(string aEndpointName);
    }
}
