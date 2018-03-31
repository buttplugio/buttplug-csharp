using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class MysteryVibeBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            ModeControl = 0,
            MotorControl = 1,
        }

        public Guid[] Services { get; } = { new Guid("f0006900-110c-478b-b74b-6f403b364a9c") };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            { (uint)Chrs.ModeControl, new Guid("f0006901-110c-478B-B74B-6F403B364A9C") },
            { (uint)Chrs.MotorControl, new Guid("f0006903-110c-478B-B74B-6F403B364A9C") },
        };

        public string[] NamePrefixes { get; } = { };

        public string[] Names { get; } =
        {
            "MV Crescendo",
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new MysteryVibe(aLogManager, aInterface, this);
        }
    }

    internal class MysteryVibe : ButtplugBluetoothDevice
    {
        internal static readonly byte[] NullSpeed = { 0, 0, 0, 0, 0, 0 };

        // This max speed seems weird, but going over it causes the device to slow down?
        internal static readonly byte MaxSpeed = 56;

        // Approximate timing delay taken from watching packet timing and testing manually.
        internal static readonly uint DelayTimeMS = 93;

        private byte[] _vibratorSpeeds = NullSpeed;
        private readonly System.Timers.Timer _updateValueTimer = new System.Timers.Timer();

        public MysteryVibe(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"MysteryVibe Crescendo",
                   aInterface,
                   aInfo)
        {
            // Create a new timer that wont fire any events just yet
            _updateValueTimer.Interval = DelayTimeMS;
            _updateValueTimer.Elapsed += MysteryVibeUpdateHandler;
            _updateValueTimer.Enabled = false;
            aInterface.DeviceRemoved += OnDeviceRemoved;

            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 6 }));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));
        }

        public override async Task<ButtplugMessage> Initialize()
        {
            BpLogger.Trace($"Initializing {Name}");

            // Kick Vibrator into motor control mode, just copying what the app sends when you go to
            // create pattern mode.
            return await Interface.WriteValue(ButtplugConsts.SystemMsgId,
                (uint)MysteryVibeBluetoothInfo.Chrs.ModeControl,
                new byte[] { 0x43, 0x02, 0x00 }, true);
        }

        private void OnDeviceRemoved(object aEvent, EventArgs aArgs)
        {
            // Timer should be turned off on removal.
            _updateValueTimer.Enabled = false;

            // Clean up event handler for that magic day when devices manage to disconnect.
            Interface.DeviceRemoved -= OnDeviceRemoved;
        }

        private async void MysteryVibeUpdateHandler(object aEvent, ElapsedEventArgs aArgs)
        {
            if (_vibratorSpeeds.SequenceEqual(NullSpeed))
            {
                _updateValueTimer.Enabled = false;
            }

            if (await Interface.WriteValue(ButtplugConsts.DefaultMsgId,
                (uint)MysteryVibeBluetoothInfo.Chrs.MotorControl,
                _vibratorSpeeds) is Error errorMsg)
            {
                BpLogger.Error($"Cannot send update to {Name}, device may stop moving.");
                _updateValueTimer.Enabled = false;
            }
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            BpLogger.Debug($"Stopping Device {Name}");
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is SingleMotorVibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await HandleVibrateCmd(
                VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, 6));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is VibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Speeds.Count < 1 || cmdMsg.Speeds.Count > 6)
            {
                return new Error(
                    "VibrateCmd requires 1-6 commands for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            var newVibratorSpeeds = (byte[])_vibratorSpeeds.Clone();

            foreach (var v in cmdMsg.Speeds)
            {
                if (v.Index > 5)
                {
                    return new Error(
                        $"Index {v.Index} is out of bounds for VibrateCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                newVibratorSpeeds[v.Index] = (byte)(v.Speed * MaxSpeed);
            }

            if (newVibratorSpeeds.SequenceEqual(_vibratorSpeeds))
            {
                return new Ok(aMsg.Id);
            }

            _vibratorSpeeds = newVibratorSpeeds;

            if (!_updateValueTimer.Enabled)
            {
                // Run the update handler once to start the command
                MysteryVibeUpdateHandler(null, null);

                // Start the timer to it will keep updating
                _updateValueTimer.Enabled = true;
            }

            return new Ok(aMsg.Id);
        }
    }
}