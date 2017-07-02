using Buttplug.Messages;
using System;
using System.Diagnostics;
using System.Timers;

namespace ButtplugKiirooEmulatorGUI
{
    internal class VibrateEventArgs : EventArgs
    {
        public readonly double VibrateValue;

        public VibrateEventArgs(double aValue)
        {
            VibrateValue = aValue;
        }
    }

    internal class KiirooMessageTranslator
    {
        private readonly Stopwatch _stopwatch;
        private readonly uint _previousSpeed;
        private readonly uint _previousPosition;
        private uint _previousKiirooPosition;
        private uint _limitedSpeed;
        private uint _currentGoalPosition;
        private uint _currentSpeed;
        private readonly Timer _vibrateTimer;
        private double _currentVibrate;

        public event EventHandler<VibrateEventArgs> VibrateEvent;

        public KiirooMessageTranslator()
        {
            _stopwatch = new Stopwatch();
            _previousSpeed = 0;
            _previousPosition = 0;
            _vibrateTimer = new Timer(20.0);
            _vibrateTimer.Elapsed += UpdateVibrate;
            _currentVibrate = 0;
        }

        ~KiirooMessageTranslator()
        {
            _vibrateTimer.Stop();
        }

        public void StartVibrateTimer()
        {
            _vibrateTimer.Start();
        }

        public void StopVibrateTimer()
        {
            _vibrateTimer.Stop();
        }

        private void UpdateVibrate(object aObject, EventArgs aEvent)
        {
            var speedModifier = .2 * (_currentSpeed / 100.0);
            if (_currentGoalPosition < 2)
            {
                speedModifier *= -1;
            }
            _currentVibrate += speedModifier;
            if (_currentVibrate > 1)
            {
                _currentVibrate = 1;
            }
            else if (_currentVibrate < 0)
            {
                _currentVibrate = 0;
            }
            VibrateEvent?.Invoke(this, new VibrateEventArgs(_currentVibrate));
        }

        public FleshlightLaunchFW12Cmd Translate(KiirooCmd aMsg)
        {
            var elapsed = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Stop();
            _currentGoalPosition = aMsg.Position;
            if (_currentGoalPosition == _previousKiirooPosition)
            {
                return new FleshlightLaunchFW12Cmd(aMsg.DeviceIndex, 0, _previousPosition, aMsg.Id);
            }
            _previousKiirooPosition = _currentGoalPosition;
            _currentSpeed = 0;

            // Speed Conversion
            if (elapsed > 2000)
            {
                _currentSpeed = 50;
            }
            else if (elapsed > 1000)
            {
                _currentSpeed = 20;
            }
            else
            {
                _currentSpeed = (uint)(100 - ((elapsed / 100) + ((elapsed / 100) * .1)));
                if (_currentSpeed > _previousSpeed)
                {
                    _currentSpeed = (_previousSpeed + ((_currentSpeed - _previousSpeed) / 6));
                }
                else if (_currentSpeed <= _previousSpeed)
                {
                    _currentSpeed = (_previousSpeed - (_currentSpeed / 2));
                }
            }
            if (_currentSpeed < 20)
            {
                _currentSpeed = 20;
            }
            _stopwatch.Start();
            // Position Conversion
            var position = (ushort)(_currentGoalPosition > 2 ? 95 : 5);
            if (elapsed <= 150)
            {
                if (_limitedSpeed == 0)
                {
                    _limitedSpeed = _currentSpeed;
                }
                return new FleshlightLaunchFW12Cmd(aMsg.DeviceIndex, _limitedSpeed, position, aMsg.Id);
            }
            _limitedSpeed = 0;
            return new FleshlightLaunchFW12Cmd(aMsg.DeviceIndex, _currentSpeed, position, aMsg.Id);
        }
    }
}