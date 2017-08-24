using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public interface IButtplugDevice
    {
        [NotNull]
        string Name { get; }

        [NotNull]
        string Identifier { get; }

        [NotNull]
        bool IsConnected { get; }

        [CanBeNull]
        event EventHandler DeviceRemoved;

        [NotNull]
        IEnumerable<Type> GetAllowedMessageTypes();

        [NotNull]
        Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg);

        [NotNull]
        Task<ButtplugMessage> Initialize();

        void Disconnect();
    }
}
