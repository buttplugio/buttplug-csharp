using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public interface IButtplugDevice
    {
        [NotNull]
        string Name { get; }

        [NotNull]
        string Identifier { get; }

        uint Index { get; set; }

        bool IsConnected { get; }

        [CanBeNull]
        event EventHandler DeviceRemoved;

        [CanBeNull]
        event EventHandler<MessageReceivedEventArgs> MessageEmitted;

        [NotNull]
        IEnumerable<Type> GetAllowedMessageTypes();

        [NotNull]
        Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg);

        [NotNull]
        Task<ButtplugMessage> Initialize();

        void Disconnect();

        [NotNull]
        MessageAttributes GetMessageAttrs(Type aMsg);
    }
}
