using System.Collections.Generic;
using System.Linq;

namespace Buttplug.Devices.Configuration
{
    public class SerialProtocolConfiguration : IProtocolConfiguration
    {
        public readonly uint BaudRate;
        public readonly byte StopBits;
        public readonly char ParityBit;
        public readonly List<string> Ports = new List<string>();

        public SerialProtocolConfiguration(uint aBaudRate, char aParityBit, byte aStopBits)
        {
            BaudRate = aBaudRate;
            ParityBit = aParityBit;
            StopBits = aStopBits;
        }

        internal SerialProtocolConfiguration(SerialInfo aConfig)
            : this(aConfig.BaudRate, aConfig.Parity, aConfig.StopBits)
        {
        }

        public void AddPortId(string aPortName)
        {
            Ports.Add(aPortName);
        }

        public bool Matches(IProtocolConfiguration aConfig)
        {
            // If our config and their config have the same ports, call it good.
            return aConfig is SerialProtocolConfiguration serialConfig && serialConfig.Ports.Union(Ports).Any();
        }
    }
}
