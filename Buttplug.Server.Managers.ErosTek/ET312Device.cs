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
        Object movementLock;

        public ET312Device(SerialPort port, IButtplugLogManager aLogManager, string name, string id)
            : base(aLogManager, name, id)
        {

            movementLock = new object();

            // Handshake with the box
            serialPort = port;
            boxkey = 0;
            Synch();

            // Setup box for remote control
            Execute(0x00);               // load default routine
            Poke(0x4083, 0x00);          // disable front panel switches
            Poke(0x409a, 0x00);          // Channel A: Gate Off
            Poke(0x419a, 0x00);          // Channel B: Gate Off
            Poke(0x40ac, 0x00);          // Channel A: Set intensity to static
            Poke(0x41ac, 0x00);          // Channel B: Set intensity to static
            Poke(0x40a5, 0x00);          // Channel A: Set intensity value
            Poke(0x41a5, 0x00);          // Channel B: Set intensity value
            Poke(0x409c, 0xff);          // stop volume ramp
            Poke(0x40b5, 0x08);          // Channel A: MA knob sets frequency
            Poke(0x41b5, 0x08);          // Channel B: MA knob sets frequency
            Poke(0x40be, 0x04);          // Channel A: Set width from advanced menu
            Poke(0x41be, 0x04);          // Channel B: Set width from advanced menu

            // Let the user know we're in control now
            WriteLCD("Buttplug", 8);
            WriteLCD("----------------", 64);

            // We're now ready to receive events
            MsgFuncs.Add(typeof(FleshlightLaunchFW12Cmd), HandleFleshlightLaunchFW12Cmd);
            MsgFuncs.Add(typeof(StopDeviceCmd), HandleStopDeviceCmd);

            // Start update timer
            updateInterval = 20;                        // <- Change this value to adjust box update frequency in ms
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

        // Timer event fired every (updateInterval) milliseconds
        private void OnUpdate(object source, System.Timers.ElapsedEventArgs e)
        {
            lock (movementLock)
            {
                try
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

                    // This is a very experimental algorithm to convert the linear "stroke"
                    // position into the very nonlinear value the ET312 needs in order
                    // to create a pleasant sensation

                    double valueA = 115 + (80 * fade) + (currentPosition * 64 / 100);
                    double valueB = 115 + (80 * fade) + ((100 - currentPosition) * 64 / 100);

                    double gamma = 1.5;

                    double correctedA = 255 * Math.Pow(valueA / 255, 1 / gamma);
                    double correctedB = 255 * Math.Pow(valueB / 255, 1 / gamma);

                    if (fade == 0)
                    {
                        correctedA = correctedB = 0;
                    }

                    Poke(0x040a5, (byte)correctedA);          // Channel A: Set intensity value
                    Poke(0x041a5, (byte)correctedB);          // Channel B: Set intensity value
                }
                catch
                {
                    AbandonShip();
                }
            }
        }

        private void AbandonShip()
        {
            lock (serialPort)
            {
                updateTimer.Enabled = false;
                serialPort.Close();
                InvokeDeviceRemoved();
            }
        }

        // Fade stim in as soon as there is movement
        private void FadeUp()
        {
            fade += updateInterval / 2000;
            fade = (fade > 1) ? 1 : fade;
        }

        // Fade stim signal out as soon as movement stops
        private void FadeDown()
        {
            fade -= updateInterval / 3000;
            fade = (fade < 0) ? 0 : fade;
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            lock (movementLock)
            {
                try
                {
                    Poke(0x040a5, 0x00);          // Channel A: Set intensity value
                    Poke(0x041a5, 0x00);          // Channel B: Set intensity value
                    position = 0;
                    speed = 0;
                    increment = 0;
                    return new Ok(aMsg.Id);
                }
                catch
                {
                    AbandonShip();
                    return new Ok(aMsg.Id);
                }
            }
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd(ButtplugDeviceMessage aMsg)
        {
            lock (movementLock)
            {
                speed = (aMsg as FleshlightLaunchFW12Cmd).Speed;
                position = (aMsg as FleshlightLaunchFW12Cmd).Position;

                position = position < 0 ? 0 : position;
                position = position > 100 ? 100 : position;
                speed = speed < 20 ? 20 : speed;
                speed = speed > 100 ? 100 : speed;

                // This is @funjack's algorithm for converting Fleshlight Launch
                // commands into absolute distance (percent) / duration (millisecond) values
                double distance = Math.Abs(position - currentPosition);
                double mil = Math.Pow(speed / 25000, -0.95);
                double duration = mil / (90 / distance);

                // We convert those into "position" increments for our OnUpdate() timer event.
                increment = 1.5 * (distance / (duration / updateInterval));

                return new Ok(aMsg.Id);
            }
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

        // Execute box command
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
                        // an already synched box without having to guess the box key
                        boxkey = (byte)(recBuffer[1] ^ 0x55);
                        Poke(0x4213, 0x10);
                        boxkey = 0x10;

                        BpLogger.Info("Handshake with ET312 successful.");
                    }
                    else
                    {
                        // Since the previous command looked like complete garbage to the box
                        // send a string of 0s to get the command parser back
                        // in sync

                        serialPort.DiscardInBuffer();
                        for (int i = 0; i < 11; i++)
                        {
                            serialPort.Write(new byte[] { 0x00 }, 0, 1);

                            try
                            {
                                if (serialPort.ReadByte() == 0x07)
                                {
                                    break;
                                }
                            }
                            catch (TimeoutException)
                            {
                                // No response? Keep trying.
                                continue;
                            }
                        }

                        // Try reading from RAM with our pre-set box key of 0x10 - if this fails, the device
                        // is in an unknown state, throw exception.
                        boxkey = 0x10;
                        Peek(0x4231);

                        // If we got this far we're back in busines!
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
