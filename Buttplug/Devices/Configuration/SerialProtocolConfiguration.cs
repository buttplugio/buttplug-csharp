using System;
using System.Collections.Generic;
using System.Linq;

namespace Buttplug.Devices.Configuration
{
    public class SerialProtocolConfiguration : IProtocolConfiguration
    {
        public uint BaudRate;
        public uint DataBits;
        public byte StopBits;
        public char ParityBit;
        public List<string> Ports = new List<string>();

        public SerialProtocolConfiguration(uint aBaudRate, uint aDataBits, char aParityBit, byte aStopBits, List<string> aPorts)
        {
            BaudRate = aBaudRate;
            DataBits = aDataBits;
            ParityBit = aParityBit;
            StopBits = aStopBits;
            Ports = aPorts ?? Ports;
        }

        // Used when we find a serial port in a serial port manager, to see if we've got a match.
        public SerialProtocolConfiguration(string aPortName)
        {
            Ports.Add(aPortName);
        }

        internal SerialProtocolConfiguration(SerialInfo aConfig)
            : this(aConfig.BaudRate, aConfig.DataBits, aConfig.Parity, aConfig.StopBits, aConfig.Ports)
        {
        }

        public bool Matches(IProtocolConfiguration aConfig)
        {
            // If our config and their config have the same ports, call it good.
            return aConfig is SerialProtocolConfiguration serialConfig && serialConfig.Ports.Intersect(Ports).Any();
        }

        public void Merge(IProtocolConfiguration aConfig)
        {
            if (!(aConfig is SerialProtocolConfiguration serialConfig))
            {
                throw new ArgumentException();
            }

            // Only allow override of ports for the moment.
            Ports = serialConfig.Ports ?? Ports;
        }
    }
}
