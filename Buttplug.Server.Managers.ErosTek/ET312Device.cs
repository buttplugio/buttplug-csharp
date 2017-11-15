using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Buttplug.Core;
using Buttplug.Core.Messages;
using static Buttplug.Server.Managers.ETSerialManager.ET312Protocol;

namespace Buttplug.Server.Managers.ETSerialManager
{
    public class ET312Device : ButtplugDevice
    {
        private SerialPort _serialPort;
        private byte _boxkey;
        private double _speed;
        private double _position;
        private double _currentPosition;
        private double _increment;
        private double _fade;
        private double _updateInterval;
        private Timer _updateTimer;
        private object _movementLock;

        public ET312Device(SerialPort port, IButtplugLogManager aLogManager, string name, string id)
            : base(aLogManager, name, id)
        {
            _movementLock = new object();

            // Handshake with the box
            _serialPort = port;
            _boxkey = 0;
            Synch();

            try
            {
                // Setup box for remote control
                Execute((byte)BoxCommand.FavouriteMode);
                Poke((uint)RAM.OutputFlags, (byte)OutputFlags.DisableControls);
                Poke((uint)RAM.ChannelAGateSelect, (byte)Gate.Off);
                Poke((uint)RAM.ChannelBGateSelect, (byte)Gate.Off);
                Poke((uint)RAM.ChannelAIntensitySelect, (byte)Select.Static);
                Poke((uint)RAM.ChannelBIntensitySelect, (byte)Select.Static);
                Poke((uint)RAM.ChannelAIntensity, 0);
                Poke((uint)RAM.ChannelBIntensity, 0);
                Poke((uint)RAM.ChannelARampValue, 255);
                Poke((uint)RAM.ChannelAFrequencySelect, (byte)Select.MA);
                Poke((uint)RAM.ChannelBFrequencySelect, (byte)Select.MA);
                Poke((uint)RAM.ChannelAWidthSelect, (byte)Select.Advanced);
                Poke((uint)RAM.ChannelBWidthSelect, (byte)Select.Advanced);

                // Let the user know we're in control now
                WriteLCD("Buttplug", 8);
                WriteLCD("----------------", 64);
            }
            catch (Exception ex)
            {
                AbandonShip();

                if (ex is ET312CommunicationException
                    || ex is InvalidOperationException
                    || ex is TimeoutException)
                {
                    // If anything goes wrong during the setup
                    // consider the entire handshake failed
                    throw new ET312HandshakeException("Failed to set up initial device parameters.");
                }

                throw ex;
            }

            // We're now ready to receive events
            MsgFuncs.Add(typeof(FleshlightLaunchFW12Cmd), HandleFleshlightLaunchFW12Cmd);
            MsgFuncs.Add(typeof(StopDeviceCmd), HandleStopDeviceCmd);

            // Start update timer
            _updateInterval = 20;                        // <- Change this value to adjust box update frequency in ms
            _updateTimer = new Timer()
            {
                Interval = _updateInterval,
                AutoReset = true,
                Enabled = true,
            };
            _updateTimer.Elapsed += OnUpdate;
        }

        /// Reset the box to defaults when application closes
        public override void Disconnect()
        {
            _updateTimer.Enabled = false;
            ResetBox();
            _serialPort.Close();
            InvokeDeviceRemoved();
        }

        // Timer event fired every (updateInterval) milliseconds
        private void OnUpdate(object source, System.Timers.ElapsedEventArgs e)
        {
            lock (_movementLock)
            {
                try
                {
                    if (_currentPosition < _position)
                    {
                        FadeUp();
                        _currentPosition += _increment;
                        _currentPosition = (_currentPosition > _position) ? _position : _currentPosition;
                    }
                    else if (_currentPosition > _position)
                    {
                        FadeUp();
                        _currentPosition -= _increment;
                        _currentPosition = (_currentPosition < _position) ? _position : _currentPosition;
                    }
                    else if (_currentPosition == _position)
                    {
                        FadeDown();
                    }

                    // This is a very experimental algorithm to convert the linear "stroke"
                    // position into the very nonlinear value the ET312 needs in order
                    // to create a pleasant sensation
                    double valueA = 115 + (80 * _fade) + (_currentPosition * 64 / 100);
                    double valueB = 115 + (80 * _fade) + ((100 - _currentPosition) * 64 / 100);

                    double gamma = 1.5;

                    double correctedA = 255 * Math.Pow(valueA / 255, 1 / gamma);
                    double correctedB = 255 * Math.Pow(valueB / 255, 1 / gamma);

                    if (_fade == 0)
                    {
                        correctedA = correctedB = 0;
                    }

                    Poke((uint)RAM.ChannelAIntensity, (byte)correctedA);          // Channel A: Set intensity value
                    Poke((uint)RAM.ChannelBIntensity, (byte)correctedB);          // Channel B: Set intensity value
                }
                catch (Exception ex)
                {
                    AbandonShip();

                    if (ex is ET312CommunicationException
                        || ex is InvalidOperationException
                        || ex is TimeoutException)
                    {
                        return;
                    }

                    throw ex;
                }
            }
        }

        // Stop communicating with the device and mark it removed
        private void AbandonShip()
        {
            lock (_serialPort)
            {
                if (_updateTimer != null)
                {
                    _updateTimer.Enabled = false;
                }

                _serialPort.Close();
                InvokeDeviceRemoved();
            }
        }

        // Fade stim in as soon as there is movement
        private void FadeUp()
        {
            _fade += _updateInterval / 2000;
            _fade = (_fade > 1) ? 1 : _fade;
        }

        // Fade stim signal out as soon as movement stops
        private void FadeDown()
        {
            _fade -= _updateInterval / 3000;
            _fade = (_fade < 0) ? 0 : _fade;
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            lock (_movementLock)
            {
                try
                {
                    Poke((uint)RAM.ChannelAIntensity, 0x00);          // Channel A: Set intensity value
                    Poke((uint)RAM.ChannelBIntensity, 0x00);          // Channel B: Set intensity value
                    _position = 0;
                    _speed = 0;
                    _increment = 0;
                    return new Ok(aMsg.Id);
                }
                catch (Exception ex)
                {
                    AbandonShip();

                    if (ex is ET312CommunicationException
                        || ex is InvalidOperationException
                        || ex is TimeoutException)
                    {
                        return new Ok(aMsg.Id);
                    }

                    throw ex;
                }
            }
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd(ButtplugDeviceMessage aMsg)
        {
            lock (_movementLock)
            {
                _speed = (aMsg as FleshlightLaunchFW12Cmd).Speed;
                _position = (aMsg as FleshlightLaunchFW12Cmd).Position;

                _position = _position < 0 ? 0 : _position;
                _position = _position > 100 ? 100 : _position;
                _speed = _speed < 20 ? 20 : _speed;
                _speed = _speed > 100 ? 100 : _speed;

                // This is @funjack's algorithm for converting Fleshlight Launch
                // commands into absolute distance (percent) / duration (millisecond) values
                double distance = Math.Abs(_position - _currentPosition);
                double mil = Math.Pow(_speed / 25000, -0.95);
                double duration = mil / (90 / distance);

                // We convert those into "position" increments for our OnUpdate() timer event.
                _increment = 1.5 * (distance / (duration / _updateInterval));

                return new Ok(aMsg.Id);
            }
        }

        // Calculates a box command checksum, sets command length and encrypts the message
        private void SendCommand(byte[] buffer)
        {
            var length = buffer.Length;

            if (length > 16)
            {
                throw new ArgumentException("Maximum command size of 16 bytes exceeded.");
            }

            // Only commands 2 bytes or longer get a checksum
            if (length > 1)
            {
                buffer[0] |= (byte)((length - 1) << 4);                     // Command length sans checksum goes into upper 4 command bits
                buffer[length - 1] = Checksum(buffer);                      // Checksum goes into last byte
            }

            // Encrypt message if key is set
            if (_boxkey != 0)
            {
                for (int c = 0; c < length; c++)
                {
                    buffer[c] = (byte)(buffer[c] ^ _boxkey);
                }
            }

            // Send message to box
            _serialPort.Write(buffer, 0, length);
        }

        // Calculates a box command checksum, sets command length and encrypts the message
        // We assume the buffer has one byte reserved at the end for the checksum
        private byte Checksum(byte[] buffer)
        {
            if (buffer.Length < 2)
            {
                throw new ArgumentException("Buffer must be at least two bytes long.");
            }

            byte sum = 0;
            for (int c = 0; c < buffer.Length - 1; c++)
            {
                sum += buffer[c];
            }

            return sum;
        }

        // Read one byte from the device
        private byte Peek(uint address)
        {
            lock (_serialPort)
            {
                byte[] sendBuffer = new byte[4];
                sendBuffer[0] = (byte)SerialCommand.Read;                               // read byte command
                sendBuffer[1] = (byte)((address & 0xff00) >> 8);    // address high byte
                sendBuffer[2] = (byte)(address & 0x00ff);           // address low byte
                SendCommand(sendBuffer);                                // encryption

                byte[] recBuffer = new byte[3];
                recBuffer[0] = (byte)_serialPort.ReadByte();              // return code
                recBuffer[1] = (byte)_serialPort.ReadByte();              // content of requested address
                recBuffer[2] = (byte)_serialPort.ReadByte();              // checksum

                // If the response is not of the expected type or Checksum doesn't match
                // consider ourselves de-synchronized. Calling Code should treat the device
                // as disconnected
                if (recBuffer[0] != ((byte)SerialResponse.Read | 0x20))
                {
                    throw new ET312CommunicationException("Unexpected return code from device.");
                }

                if (recBuffer[2] != Checksum(recBuffer))
                {
                    throw new ET312CommunicationException("Checksum error in reply from device.");
                }

                return recBuffer[1];
            }
        }

        // TODO: Replace this by some kind of asynchronous processing so waiting for
        //       the response from the box doesn't hold up the entire event loop
        private void Poke(uint address, byte value)
        {
            lock (_serialPort)
            {
                byte[] sendBuffer = new byte[5];
                sendBuffer[0] = (byte)SerialCommand.Write;          // write byte command
                sendBuffer[1] = (byte)((address & 0xff00) >> 8);    // address high byte
                sendBuffer[2] = (byte)(address & 0x00ff);           // address low byte
                sendBuffer[3] = value;                              // value
                SendCommand(sendBuffer);                                // encryption

                // If the response is not ACK, consider ourselves de-synchronized.
                // Calling Code should treat the device as disconnected
                if (_serialPort.ReadByte() != (byte)SerialResponse.OK)
                {
                    throw new ET312CommunicationException("Unexpected return code from device.");
                }
            }
        }

        // Execute box command
        private void Execute(byte command)
        {
            Poke((uint)RAM.BoxCommand1, command);
            System.Threading.Thread.Sleep(18);
        }

        // Write Message to the LCD display
        private void WriteLCD(string message, int offset)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            for (int c = 0; c < buffer.Length; c++)
            {
                Poke((uint)RAM.WriteLCDParameter, buffer[c]);
                Poke((uint)RAM.WriteLCDPosition, (byte)(offset + c));
                Execute((byte)BoxCommand.LCDWriteCharacter);
            }
        }

        // Warm Start the Box
        private byte ResetBox()
        {
            lock (_serialPort)
            {
                byte[] sendBuffer = new byte[3];
                sendBuffer[0] = (byte)SerialCommand.Reset;          // reset command
                sendBuffer[1] = 0x55;                               // parameter for reset is always 0x55
                SendCommand(sendBuffer);                            // encryption

                return (byte)_serialPort.ReadByte();
            }
        }

        // Set up serial XOR encryption - mandatory for write accesss
        private void Synch()
        {
            lock (_serialPort)
            {
                BpLogger.Info("Attempting Synch");
                _boxkey = 0;

                try
                {
                    // Try to read a byte from RAM using an unencrypted command
                    // If this works, we are not synched yet
                    byte[] sendBuffer = new byte[4];
                    sendBuffer[0] = (byte)SerialCommand.Read;           // read byte command
                    sendBuffer[1] = 0;                                  // address high byte
                    sendBuffer[2] = 0;                                  // address low byte
                    SendCommand(sendBuffer);

                    byte result = (byte)_serialPort.ReadByte();

                    if (result == ((byte)SerialResponse.Read | 0x20))
                    {
                        // It worked! Discard the rest of the response
                        _serialPort.ReadByte();
                        _serialPort.ReadByte();

                        // Commence handshake
                        BpLogger.Info("Encryption is not yet set up. Send Handshake.");

                        sendBuffer = new byte[3];
                        sendBuffer[0] = (byte)SerialCommand.KeyExchange;    // synch command
                        sendBuffer[1] = 0;                                  // our key
                        SendCommand(sendBuffer);

                        // Receive box key
                        byte[] recBuffer = new byte[3];
                        recBuffer[0] = (byte)_serialPort.ReadByte();                      // return code
                        recBuffer[1] = (byte)_serialPort.ReadByte();                      // box key
                        recBuffer[2] = (byte)_serialPort.ReadByte();                      // checksum

                        // Response valid?
                        if (recBuffer[0] != ((byte)SerialResponse.KeyExchange | 0x20))
                        {
                            throw new ET312CommunicationException("Unexpected return code from device.");
                        }

                        if (recBuffer[2] != Checksum(recBuffer))
                        {
                            throw new ET312CommunicationException("Checksum error in reply from device.");
                        }

                        _boxkey = (byte)(recBuffer[1] ^ 0x55);

                        // Override the random box key with our own (0x10) so we can reconnect to
                        // an already synched box without having to guess the box key
                        Poke((uint)RAM.BoxKey, 0x10);
                        _boxkey = 0x10;

                        BpLogger.Info("Handshake with ET312 successful.");
                    }
                    else
                    {
                        // Since the previous command looked like complete garbage to the box
                        // send a string of 0s to get the command parser back
                        // in sync
                        _serialPort.DiscardInBuffer();
                        for (int i = 0; i < 11; i++)
                        {
                            _serialPort.Write(new byte[] { (byte)SerialCommand.Sync }, 0, 1);

                            try
                            {
                                if (_serialPort.ReadByte() == (byte)SerialResponse.Error)
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
                        _boxkey = 0x10;
                        Peek((uint)Flash.BoxModel);

                        // If we got this far we're back in busines!
                        BpLogger.Info("Encryption already set up. No handshake required.");
                    }
                }
                catch (Exception ex)
                {
                    BpLogger.Info("Synch with ET312 Failed");
                    AbandonShip();

                    if (ex is ET312CommunicationException
                        || ex is InvalidOperationException
                        || ex is TimeoutException)
                    {
                            throw new ET312HandshakeException("Failed to set up communications with device.");
                    }

                    throw ex;
                }
            }
        }
    }
}
