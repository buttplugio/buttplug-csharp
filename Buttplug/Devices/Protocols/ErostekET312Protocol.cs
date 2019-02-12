// <copyright file="ErostekET312Device.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Server.Util;
using Timer = System.Timers.Timer;

namespace Buttplug.Devices.Protocols
{
    public class ErostekET312Protocol : ButtplugDeviceProtocol
    {
        private byte _boxKey;
        private SemaphoreSlim _lock = new SemaphoreSlim(1);
        private double _updateInterval = 20;  // <- Change this value to adjust box update frequency in ms

        private Timer _updateTimer;
        private double _speed;
        private double _position;
        private double _currentPosition;
        private double _increment;
        private double _fade;

        public ErostekET312Protocol(IButtplugLogManager aLogManager, IButtplugDeviceImpl aDevice)
        : base(aLogManager, $"Erostek ET312 - {aDevice.Name}", aDevice)
        {
        }

        public override async Task InitializeAsync(CancellationToken aToken)
        {
            BpLogger.Info("Attempting Synch");
            _boxKey = 0;

            // We shouldn't need to lock here, as this is called before any other commands can be sent.
            try
            {
                // Try to read a byte from RAM using an unencrypted command. If this works, we are
                // not synched yet.
                await SendCommand(new byte[]
                {
                    (byte)ErostekET312ProtocolConsts.SerialCommand.Read, // read byte command
                    0, // address high byte
                    0, // address low byte
                }, aToken).ConfigureAwait(false);

                var result = await Interface.ReadValueAsync(new ButtplugDeviceReadOptions { ReadLength = 1 }, aToken).ConfigureAwait(false);

                if (result.Length == 1 && result[0] == ((byte)ErostekET312ProtocolConsts.SerialResponse.Read | 0x20))
                {
                    await InitUnsynced(aToken).ConfigureAwait(false);
                }
                else
                {
                    await InitSynced(aToken).ConfigureAwait(false);
                }

                // Setup box for remote control
                await Execute((byte)ErostekET312ProtocolConsts.BoxCommand.FavouriteMode, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelAGateSelect, (byte)ErostekET312ProtocolConsts.Gate.Off, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelBGateSelect, (byte)ErostekET312ProtocolConsts.Gate.Off, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelAIntensitySelect, (byte)ErostekET312ProtocolConsts.Select.Static, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelBIntensitySelect, (byte)ErostekET312ProtocolConsts.Select.Static, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelAIntensity, 0, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelBIntensity, 0, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelARampValue, 255, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelAFrequencySelect, (byte)ErostekET312ProtocolConsts.Select.MA, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelBFrequencySelect, (byte)ErostekET312ProtocolConsts.Select.MA, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelAWidthSelect, (byte)ErostekET312ProtocolConsts.Select.Advanced, aToken).ConfigureAwait(false);
                await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelBWidthSelect, (byte)ErostekET312ProtocolConsts.Select.Advanced, aToken).ConfigureAwait(false);

                // WriteLCD has its own lock.
                _lock.Release();

                // Let the user know we're in control now
                await WriteLCD("Buttplug", 8, aToken).ConfigureAwait(false);
                await WriteLCD("----------------", 64, aToken).ConfigureAwait(false);

                // We're now ready to receive events
                AddMessageHandler<FleshlightLaunchFW12Cmd>(HandleFleshlightLaunchCmd);
                AddMessageHandler<LinearCmd>(HandleLinearCmd, new MessageAttributes { FeatureCount = 1 });
                AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);

                // Start update timer
                _updateTimer = new Timer()
                {
                    Interval = _updateInterval,
                    AutoReset = true,
                    Enabled = true,
                };
                _updateTimer.Elapsed += OnUpdate;
            }
            catch (Exception ex)
            {
                BpLogger.Error("Synch with ET312 Failed");

                // Release the lock before disconnecting
                await DisconnectInternal(false, default(CancellationToken)).ConfigureAwait(false);

                if (ex is InvalidOperationException
                    || ex is TimeoutException)
                {
                    throw new ButtplugDeviceException(BpLogger, "Failed to initialize device.");
                }

                throw;
            }
        }

        private async Task InitUnsynced(CancellationToken aToken)
        {
            // It worked! Discard the rest of the response
            await Interface.ReadValueAsync(new ButtplugDeviceReadOptions { ReadLength = 2 }, aToken).ConfigureAwait(false);

            // Commence handshake
            BpLogger.Info("Encryption is not yet set up. Send Handshake.");

            await SendCommand(new byte[]
            {
                (byte)ErostekET312ProtocolConsts.SerialCommand.KeyExchange, // synch command
                0, // our key
            }, aToken).ConfigureAwait(false);

            // Receive box key
            // byte 0 - return code
            // byte 1 - box key
            // byte 2 - checksum
            var recBuffer = await Interface.ReadValueAsync(new ButtplugDeviceReadOptions { ReadLength = 3 }, aToken).ConfigureAwait(false);

            // Response valid?
            if (recBuffer[0] != ((byte)ErostekET312ProtocolConsts.SerialResponse.KeyExchange | 0x20))
            {
                throw new ButtplugDeviceException(BpLogger, "Unexpected return code from device.");
            }

            if (recBuffer[2] != Checksum(recBuffer))
            {
                throw new ButtplugDeviceException(BpLogger, "Checksum error in reply from device.");
            }

            _boxKey = (byte)(recBuffer[1] ^ 0x55);

            // Override the random box key with our own (0x10) so we can reconnect to an already
            // synched box without having to guess the box key
            await Poke((uint)ErostekET312ProtocolConsts.RAM.BoxKey, 0x10, aToken).ConfigureAwait(false);
            _boxKey = 0x10;

            BpLogger.Info("Handshake with ET312 successful.");
        }

        private async Task InitSynced(CancellationToken aToken)
        {
            // Since the previous command looked like complete garbage to the box send a string of 0s
            // to get the command parser back in sync
            // Clear();
            for (var i = 0; i < 11; i++)
            {
                await Interface.WriteValueAsync(new[] { (byte)ErostekET312ProtocolConsts.SerialCommand.Sync }, aToken).ConfigureAwait(false);

                try
                {
                    // This read will fail at least once, so we can't use ReadAsync here since it
                    // doesn't currently provide a timeout. However, we can expose ReadByte() on the
                    // backing stream and use that.
                    var errByte = await Interface.ReadValueAsync(new ButtplugDeviceReadOptions { ReadLength = 1 }, aToken);
                    if (errByte.Length == 0 || errByte[0] == -1)
                    {
                        continue;
                    }

                    if (errByte[0] == (byte)ErostekET312ProtocolConsts.SerialResponse.Error)
                    {
                        break;
                    }
                }
                catch (TimeoutException)
                {
                    // No response? Keep trying.
                }
            }

            // Try reading from RAM with our pre-set box key of 0x10 - if this fails, the device is
            // in an unknown state, throw an exception.
            _boxKey = 0x10;
            await Peek((uint)ErostekET312ProtocolConsts.Flash.BoxModel, aToken).ConfigureAwait(false);

            // If we got this far we're back in business!
            BpLogger.Info("Encryption already set up. No handshake required.");
        }

        private async Task DisconnectInternal(bool aShouldReset, CancellationToken aToken)
        {
            if (_updateTimer != null)
            {
                _updateTimer.Enabled = false;
            }

            if (!Interface.Connected)
            {
                return;
            }

            try
            {
                await _lock.WaitAsync(aToken).ConfigureAwait(false);

                if (aShouldReset)
                {
                    await ResetBox(aToken).ConfigureAwait(false);
                }

                Interface.Disconnect();
            }
            finally
            {
                _lock.Release();
            }
        }

        // Calculates a box command checksum, sets command length and encrypts the message
        private async Task SendCommand(byte[] buffer, CancellationToken aToken)
        {
            if (buffer.Length > 16)
            {
                throw new ArgumentException("Maximum command size of 16 bytes exceeded.");
            }

            // Only commands 2 bytes or longer get a checksum
            if (buffer.Length > 1)
            {
                // Resize the buffer we were sent to accommodate a checksum.
                Array.Resize(ref buffer, buffer.Length + 1);
                buffer[0] |= (byte)((buffer.Length - 1) << 4);                     // Command length sans checksum goes into upper 4 command bits
                buffer[buffer.Length - 1] = Checksum(buffer);                      // Checksum goes into last byte
            }

            // Encrypt message if key is set
            if (_boxKey != 0)
            {
                for (var c = 0; c < buffer.Length; c++)
                {
                    buffer[c] = (byte)(buffer[c] ^ _boxKey);
                }
            }

            // Send message to box
            await Interface.WriteValueAsync(buffer, aToken).ConfigureAwait(false);
        }

        // Calculates a box command checksum, sets command length and encrypts the message We assume
        // the buffer has one byte reserved at the end for the checksum
        private byte Checksum(byte[] buffer)
        {
            if (buffer.Length < 2)
            {
                throw new ArgumentException("Buffer must be at least two bytes long.");
            }

            byte sum = 0;
            for (var c = 0; c < buffer.Length - 1; c++)
            {
                sum += buffer[c];
            }

            return sum;
        }

        // Read one byte from the device
        // ReSharper disable once UnusedMethodReturnValue.Local
        private async Task<byte> Peek(uint address, CancellationToken aToken)
        {
            await SendCommand(new[]
            {
                (byte)ErostekET312ProtocolConsts.SerialCommand.Read, // read byte command
                (byte)((address & 0xff00) >> 8), // address high byte
                (byte)(address & 0x00ff), // address low byte
            }, aToken).ConfigureAwait(false);

            // byte 0 - return code
            // byte 1 - content of requested address
            // byte 2 - checksum
            var recBuffer = await Interface.ReadValueAsync(new ButtplugDeviceReadOptions { ReadLength = 3 }, aToken).ConfigureAwait(false);

            // If the response is not of the expected type or Checksum doesn't match consider
            // ourselves de-synchronized. Calling Code should treat the device as disconnected
            if (recBuffer[0] != ((byte)ErostekET312ProtocolConsts.SerialResponse.Read | 0x20))
            {
                throw new ButtplugDeviceException(BpLogger, "Unexpected return code from device.");
            }

            if (recBuffer[2] != Checksum(recBuffer))
            {
                throw new ButtplugDeviceException(BpLogger, "Checksum error in reply from device.");
            }

            return recBuffer[1];
        }

        private async Task Poke(uint address, byte value, CancellationToken aToken)
        {
            await SendCommand(new[]
            {
                (byte)ErostekET312ProtocolConsts.SerialCommand.Write,          // write byte command
                (byte)((address & 0xff00) >> 8),    // address high byte
                (byte)(address & 0x00ff),           // address low byte
                value,                              // value
            }, aToken).ConfigureAwait(false);

            // If the response is not ACK, consider ourselves de-synchronized. Calling Code should
            // treat the device as disconnected
            var statusByte = await Interface.ReadValueAsync(new ButtplugDeviceReadOptions { ReadLength = 1 }, aToken).ConfigureAwait(false);
            if (statusByte.Length != 1 || statusByte[0] != (byte)ErostekET312ProtocolConsts.SerialResponse.OK)
            {
                throw new ButtplugDeviceException(BpLogger, "Unexpected return code from device.");
            }
        }

        // Execute box command
        private async Task Execute(byte command, CancellationToken aToken)
        {
            await Poke((uint)ErostekET312ProtocolConsts.RAM.BoxCommand1, command, aToken).ConfigureAwait(false);
            Thread.Sleep(18);
        }

        // Write Message to the LCD display
        // ReSharper disable once InconsistentNaming
        private async Task WriteLCD(string message, int offset, CancellationToken aToken)
        {
            var buffer = Encoding.ASCII.GetBytes(message);
            for (var c = 0; c < buffer.Length; c++)
            {
                try
                {
                    await _lock.WaitAsync().ConfigureAwait(false);
                    await Poke((uint)ErostekET312ProtocolConsts.RAM.WriteLCDParameter, buffer[c], aToken).ConfigureAwait(false);
                    await Poke((uint)ErostekET312ProtocolConsts.RAM.WriteLCDPosition, (byte)(offset + c), aToken).ConfigureAwait(false);
                    await Execute((byte)ErostekET312ProtocolConsts.BoxCommand.LCDWriteCharacter, aToken).ConfigureAwait(false);
                }
                finally
                {
                    _lock.Release();
                }
            }
        }

        // Warm Start the Box
        // ReSharper disable once UnusedMethodReturnValue.Local
        private async Task<byte> ResetBox(CancellationToken aToken)
        {
            await SendCommand(new byte[]
            {
                (byte)ErostekET312ProtocolConsts.SerialCommand.Reset,          // reset command
                0x55,                               // parameter for reset is always 0x55
            }, aToken).ConfigureAwait(false);

            var retBuf = await Interface.ReadValueAsync(new ButtplugDeviceReadOptions { ReadLength = 1 }, aToken).ConfigureAwait(false);
            return retBuf[0];
        }

        // Timer event fired every (updateInterval) milliseconds. This is an event handler, so async
        // void is ok here.
        private async void OnUpdate(object source, ElapsedEventArgs e)
        {
            try
            {
                await _lock.WaitAsync().ConfigureAwait(false);
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
                    else
                    {
                        FadeDown();
                    }

                    // This is a very experimental algorithm to convert the linear "stroke" position
                    // into the very nonlinear value the ET312 needs in order to create a pleasant sensation
                    var valueA = 115 + (80 * _fade) + (_currentPosition * 64 / 100);
                    var valueB = 115 + (80 * _fade) + ((100 - _currentPosition) * 64 / 100);

                    var gamma = 1.5;

                    var correctedA = 255 * Math.Pow(valueA / 255, 1 / gamma);
                    var correctedB = 255 * Math.Pow(valueB / 255, 1 / gamma);

                    if (Math.Abs(_fade) < 0.0001)
                    {
                        correctedA = correctedB = 0;
                    }

                    await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelAIntensity,
                        (byte)correctedA, default(CancellationToken)).ConfigureAwait(false); // Channel A: Set intensity value
                    await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelBIntensity,
                        (byte)correctedB, default(CancellationToken)).ConfigureAwait(false); // Channel B: Set intensity value
                }
                catch (Exception ex)
                {
                    _lock.Release();
                    await DisconnectInternal(false, default(CancellationToken)).ConfigureAwait(false);

                    if (ex is ButtplugDeviceException
                        || ex is InvalidOperationException
                        || ex is TimeoutException)
                    {
                        return;
                    }

                    throw;
                }
            }
            finally
            {
                _lock.Release();
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

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            try
            {
                await _lock.WaitAsync(aToken).ConfigureAwait(false);
                try
                {
                    await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelAIntensity, 0x00, aToken).ConfigureAwait(false); // Channel A: Set intensity value
                    await Poke((uint)ErostekET312ProtocolConsts.RAM.ChannelBIntensity, 0x00, aToken).ConfigureAwait(false); // Channel B: Set intensity value
                    _position = 0;
                    _speed = 0;
                    _increment = 0;
                    return new Ok(aMsg.Id);
                }
                catch (Exception ex)
                {
                    _lock.Release();
                    await DisconnectInternal(false, aToken).ConfigureAwait(false);

                    if (ex is ButtplugDeviceException
                        || ex is InvalidOperationException
                        || ex is TimeoutException)
                    {
                        return new Ok(aMsg.Id);
                    }

                    throw;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<ButtplugMessage> HandleLinearCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<LinearCmd>(aMsg);

            if (cmdMsg.Vectors.Count != 1)
            {
                throw new ButtplugDeviceException(BpLogger,
                    "LinearCmd requires 1 vector for this device.",
                    cmdMsg.Id);
            }

            foreach (var v in cmdMsg.Vectors)
            {
                if (v.Index != 0)
                {
                    throw new ButtplugDeviceException(BpLogger,
                        $"Index {v.Index} is out of bounds for LinearCmd for this device.",
                        cmdMsg.Id);
                }

                return await HandleFleshlightLaunchCmd(new FleshlightLaunchFW12Cmd(cmdMsg.DeviceIndex,
                    Convert.ToUInt32(FleshlightHelper.GetSpeed(Math.Abs((_position / 100) - v.Position), v.Duration) * 99),
                    Convert.ToUInt32(v.Position * 99), cmdMsg.Id), aToken).ConfigureAwait(false);
            }

            return new Ok(aMsg.Id);
        }

        private Task<ButtplugMessage> HandleFleshlightLaunchCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<FleshlightLaunchFW12Cmd>(aMsg);

            _speed = (Convert.ToDouble(cmdMsg.Speed) / 99) * 100;
            _position = (Convert.ToDouble(cmdMsg.Position) / 99) * 100;

            _position = _position < 0 ? 0 : _position;
            _position = _position > 100 ? 100 : _position;
            _speed = _speed < 20 ? 20 : _speed;
            _speed = _speed > 100 ? 100 : _speed;

            // This is @funjack's algorithm for converting Fleshlight Launch commands into absolute
            // distance (percent) / duration (millisecond) values
            var distance = Math.Abs(_position - _currentPosition);
            var duration = FleshlightHelper.GetDuration(distance / 100, _speed / 100);

            // We convert those into "position" increments for our OnUpdate() timer event.
            _increment = 1.5 * (distance / (duration / _updateInterval));

            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }
    }
}
