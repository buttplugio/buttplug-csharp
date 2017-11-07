using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Managers.ETSerialManager
{

    public class ET312HandshakeException : Exception
    {
        public ET312HandshakeException()
        {
        }

        public ET312HandshakeException(string message)
            : base(message)
        {
        }

        public ET312HandshakeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class ET312CommunicationException : Exception
    {
        public ET312CommunicationException()
        {
        }

        public ET312CommunicationException(string message)
            : base(message)
        {
        }

        public ET312CommunicationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class ET312Device : ButtplugDevice
    {
        private SerialPort serialPort;
        private byte boxkey;
        private double speed;
        private double position;
        private double currentPosition;
        private double increment;
        private double fade;
        private double updateInterval;
        private Timer updateTimer;

        public ET312Device(SerialPort port, IButtplugLogManager aLogManager, string name, string id)
            : base(aLogManager, name, id)
        {

            // Handshake with the box
            serialPort = port;
            boxkey = 0;
            Synch();

            // Setup box for remote control
            Execute(0x00);                // load default routine
            Poke(0x409c, 0xff);           // stop volume ramp
            Poke(0x4083, 0x00);           // disable front panel switches
            Poke(0x040b5, 0x08);          // Channel A: MA knob sets frequency
            Poke(0x041b5, 0x08);          // Channel B: MA knob sets frequency
            Poke(0x040be, 0x04);          // Channel A: Set width from advanced menu
            Poke(0x041be, 0x04);          // Channel B: Set width from advanced menu
            Poke(0x040ac, 0x00);          // Channel A: Set intensity to static
            Poke(0x041ac, 0x00);          // Channel B: Set intensity to static
            Poke(0x0409a, 0x00);          // Channel A: Gate Off
            Poke(0x0419a, 0x00);          // Channel B: Gate Off
            Poke(0x040a5, 0x00);          // Channel A: Set intensity value
            Poke(0x041a5, 0x00);          // Channel B: Set intensity value

            // Let the user know we're in control now
            WriteLCD("Buttplug", 8);
            WriteLCD("----------------", 64);

            // We're not ready to receive events
            MsgFuncs.Add(typeof(FleshlightLaunchFW12Cmd), HandleFleshlightLaunchFW12Cmd);

            // Start update timer
            updateInterval = 20;
            updateTimer = new System.Timers.Timer();
            updateTimer.Interval = updateInterval;
            updateTimer.Elapsed += OnUpdate;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        private void OnUpdate(object source, System.Timers.ElapsedEventArgs e)
        {
            if (currentPosition < position)
            {
                FadeUp();
                currentPosition += increment;
                currentPosition = (currentPosition > position) ? position : currentPosition;
            }
            else if (currentPosition > position)
            {
                FadeUp();
                currentPosition -= increment;
                currentPosition = (currentPosition < position) ? position : currentPosition;
            }
            else if (currentPosition == position)
            {
                FadeDown();
            }

            double valueA = 192 + (currentPosition * 64 / 100);
            double valueB = 192 + ((100 - currentPosition) * 64 / 100);

            Poke(0x040a5, (byte)(valueA * fade));          // Channel A: Set intensity value
            Poke(0x041a5, (byte)(valueB * fade));          // Channel B: Set intensity value
        }

        private void FadeUp()
        {
            fade += updateInterval / 2000;
            fade = (fade > 1) ? 1 : fade;
        }

        private void FadeDown()
        {
            fade -= updateInterval / 4000;
            fade = (fade < 0) ? 0 : fade;
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            Poke(0x040a5, 0x00);          // Channel A: Set intensity value
            Poke(0x041a5, 0x00);          // Channel B: Set intensity value
            position = 0;
            speed = 0;
            increment = 0;
            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd(ButtplugDeviceMessage aMsg)
        {
            speed = (aMsg as FleshlightLaunchFW12Cmd).Speed;
            position = (aMsg as FleshlightLaunchFW12Cmd).Position;

            position = position < 0 ? 0 : position;
            position = position > 100 ? 100 : position;
            speed = speed < 20 ? 20 : speed;
            speed = speed > 100 ? 100 : speed;

            double distance = Math.Abs(position - currentPosition);
            double mil = Math.Pow(speed / 25000, -0.95);
            double duration = mil / (90 / distance);

            increment = distance * updateInterval / duration;
            return new Ok(aMsg.Id);
        }

        // Calculates a box command checksum
        // We assume the buffer has one byte reserved at the end for the checksum
        private byte Checksum(byte[] buffer)
        {
            if (buffer.Length < 2)
            {
                throw new ArgumentException();
            }

            byte sum = 0;
            for (int c = 0; c < buffer.Length - 1; c++)
            {
                sum += buffer[c];
            }

            return sum;
        }

        // Encrypt a box command with the device's box key
        private void SillyXOR(byte[] buffer)
        {
            for (int c = 0; c < buffer.Length; c++)
            {
                buffer[c] = (byte)(buffer[c] ^ boxkey);
            }
        }

        // Read one byte from the device
        private byte Peek(uint address)
        {
            lock (serialPort)
            {
                byte[] sendBuffer = new byte[4];
                sendBuffer[0] = 0x3c;                               // read byte command
                sendBuffer[1] = (byte)((address & 0xff00) >> 8);    // address high byte
                sendBuffer[2] = (byte)(address & 0x00ff);           // address low byte
                sendBuffer[3] = Checksum(sendBuffer);               // checksum
                SillyXOR(sendBuffer);                               // encryption

                serialPort.Write(sendBuffer, 0, 4);

                byte[] recBuffer = new byte[3];
                recBuffer[0] = (byte)serialPort.ReadByte();              // return code
                recBuffer[1] = (byte)serialPort.ReadByte();              // content of requested address
                recBuffer[2] = (byte)serialPort.ReadByte();              // checksum

                // If the response is not of the expected type or Checksum doesn't match
                // consider ourselves de-synchronized. Calling Code should treat the device
                // as disconnected
                if (recBuffer[0] != 0x22 || recBuffer[2] != Checksum(recBuffer))
                {
                    throw new ET312CommunicationException();
                }

                return recBuffer[1];
            }
        }

        // TODO: Replace this by some kind of asynchronous processing so waiting for
        //       the response from the box doesn't hold up the entire event loop
        private void Poke(uint address, byte value)
        {
            lock (serialPort)
            {
                byte[] sendBuffer = new byte[5];
                sendBuffer[0] = 0x4d;                               // write byte command
                sendBuffer[1] = (byte)((address & 0xff00) >> 8);    // address high byte
                sendBuffer[2] = (byte)(address & 0x00ff);           // address low byte
                sendBuffer[3] = value;                              // value
                sendBuffer[4] = Checksum(sendBuffer);               // checksum
                SillyXOR(sendBuffer);                               // encryption

                serialPort.Write(sendBuffer, 0, 5);

                // If the response is not ACK, consider ourselves de-synchronized.
                // Calling Code should treat the device as disconnected
                if (serialPort.ReadByte() != 0x06)
                {
                    throw new ET312CommunicationException();
                }
            }
        }

        // Execute one or two box commands
        private void Execute(byte command)
        {
            Poke(0x4070, command);
            System.Threading.Thread.Sleep(18);
        }

        // Write Message to the LCD display
        private void WriteLCD(string message, int offset)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            for (int c = 0; c < buffer.Length; c++)
            {
                Poke(0x4180, buffer[c]);              // ASCII Value
                Poke(0x4181, (byte)(offset + c));     // Offset (+64 = second line)
                Execute(0x13);                        // Write Character
            }
        }

        // Set up serial XOR encryption - mandatory for write accesss
        private void Synch()
        {
            lock (serialPort)
            {
                BpLogger.Info("Attempting Synch");

                try
                {
                    // Try to read a byte from RAM using an unencrypted command
                    // If this works, we are not synched yet
                    byte[] sendBuffer = new byte[4];
                    sendBuffer[0] = 0x3c;                               // read byte command
                    sendBuffer[1] = 0x42;                               // address high byte
                    sendBuffer[2] = 0x31;                               // address low byte
                    sendBuffer[3] = Checksum(sendBuffer);               // checksum
                    serialPort.Write(sendBuffer, 0, 4);

                    byte result = (byte)serialPort.ReadByte();

                    if (result == 0x22)
                    {
                        // It worked! Discard the rest of the response
                        serialPort.ReadByte();
                        serialPort.ReadByte();

                        // Commence handshake
                        BpLogger.Info("Encryption is not yet set up. Send Handshake.");

                        sendBuffer = new byte[3];
                        sendBuffer[0] = 0x2f;                               // synch command
                        sendBuffer[1] = 0x00;                               // our key
                        sendBuffer[2] = Checksum(sendBuffer);               // checksum
                        serialPort.Write(sendBuffer, 0, 3);

                        // Receive box key
                        byte[] recBuffer = new byte[3];
                        recBuffer[0] = (byte)serialPort.ReadByte();                      // return code
                        recBuffer[1] = (byte)serialPort.ReadByte();                      // box key
                        recBuffer[2] = (byte)serialPort.ReadByte();                      // checksum

                        // Response valid?
                        if (recBuffer[0] != 0x21 || recBuffer[2] != Checksum(recBuffer))
                        {
                            throw new ET312CommunicationException();
                        }

                        // Override the random box key with our own (0x10) so we can reconnect to
                        // an already synched box without having to guess the boy key
                        boxkey = (byte)(recBuffer[1] ^ 0x55);
                        Poke(0x4213, 0x10);
                        boxkey = 0x10;

                        BpLogger.Info("Handshake with ET312 successful.");
                    }
                    else
                    {
                        // Since the previous command looked like complete garbage to the box
                        // send this string of 0s to get the command parses in the box back
                        // in sync
                        serialPort.Write("\0\0\0\0\0\0\0\0\0\0\0");
                        serialPort.ReadByte();

                        // Try reading from RAM with our pre-set box key of 0x10 - if this fails, the device
                        // is in an unknown state, throw exception.
                        boxkey = 0x10;
                        Peek(0x4231);

                        // If we get this far we're back in busines!
                        BpLogger.Info("Encryption already set up. No handshake required.");
                    }
                }
                catch
                {
                    BpLogger.Info("Synch with ET312 Failed");
                    throw new ET312HandshakeException();
                }
            }
        }
    }
}
