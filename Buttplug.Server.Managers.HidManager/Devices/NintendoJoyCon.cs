using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using HidLibrary;
using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;

namespace Buttplug.Server.Managers.HidManager.Devices
{
    internal struct Rumble
    {
        public float h_f, amp, l_f;
        public float t;
        public bool timed_rumble;

        public void set_vals(float low_freq, float high_freq, float amplitude, int time = 0)
        {
            h_f = high_freq;
            amp = amplitude;
            l_f = low_freq;
            timed_rumble = false;
            t = 0;
            if (time != 0)
            {
                t = time / 1000f;
                timed_rumble = true;
            }
        }

        public Rumble(float low_freq, float high_freq, float amplitude, int time = 0)
        {
            h_f = high_freq;
            amp = amplitude;
            l_f = low_freq;
            timed_rumble = false;
            t = 0;
            if (time != 0)
            {
                t = time / 1000f;
                timed_rumble = true;
            }
        }

        private float clamp(float x, float min, float max)
        {
            if (x < min)
            {
                return min;
            }

            if (x > max)
            {
                return max;
            }

            return x;
        }

        public byte[] GetData()
        {
            var rumble_data = new byte[8];
            l_f = clamp(l_f, 40.875885f, 626.286133f);
            amp = clamp(amp, 0.0f, 1.0f);
            h_f = clamp(h_f, 81.75177f, 1252.572266f);
            var hf = (UInt16)((Math.Round(32f * Math.Log(h_f * 0.1f, 2)) - 0x60) * 4);
            byte lf = (byte)(Math.Round(32f * Math.Log(l_f * 0.1f, 2)) - 0x40);
            byte hf_amp;
            if (amp == 0) { hf_amp = 0; }

            else if (amp < 0.117) { hf_amp = (byte)(((Math.Log(amp * 1000, 2) * 32) - 0x60) / (5 - Math.Pow(amp, 2)) - 1); }
            else if (amp < 0.23) { hf_amp = (byte)(((Math.Log(amp * 1000, 2) * 32) - 0x60) - 0x5c); }
            else { hf_amp = (byte)((((Math.Log(amp * 1000, 2) * 32) - 0x60) * 2) - 0xf6); }

            var lf_amp = (UInt16)((hf_amp) * .5);
            byte parity = (byte)(lf_amp % 2);
            if (parity > 0)
            {
                --lf_amp;
            }

            lf_amp = (UInt16)(lf_amp >> 1);
            lf_amp += 0x40;
            if (parity > 0) { lf_amp |= 0x8000; }
            rumble_data = new byte[8];
            rumble_data[0] = (byte)(hf & 0xff);
            rumble_data[1] = (byte)((hf >> 8) & 0xff);
            rumble_data[2] = lf;
            rumble_data[1] += hf_amp;
            rumble_data[2] += (byte)((lf_amp >> 8) & 0xff);
            rumble_data[3] += (byte)(lf_amp & 0xff);
            for (int i = 0; i < 4; ++i)
            {
                rumble_data[4 + i] = rumble_data[i];
            }
            //Console.WriteLine(string.Format("Encoded hex freq: {0:X2}", encoded_hex_freq));
            //Debug.Log(string.Format("lf_amp: {0:X4}", lf_amp));
            //Debug.Log(string.Format("hf_amp: {0:X2}", hf_amp));
            //Debug.Log(string.Format("l_f: {0:F}", l_f));
            //Debug.Log(string.Format("hf: {0:X4}", hf));
            //Debug.Log(string.Format("lf: {0:X2}", lf));
            return rumble_data;
        }
    }

    internal class NintendoJoyConHidDeviceInfo : IHidDeviceInfo
    {
        public string Name { get; } = "Nintendo JoyCon (L)";

        public int VendorId { get; } = 0x057e;

        public int ProductId { get; } = 0x2006;

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager, IHidDevice aHid)
        {
            return new NintendoJoyCon(aLogManager, aHid, this);
        }
    }

    internal class NintendoJoyCon : HidButtplugDevice
    {
        public enum DebugType : int
        {
            NONE = 0,
            ALL,
            COMMS,
            THREADING,
            IMU,
            RUMBLE,
            CUSTOM1,
        };
        public DebugType debug_type = DebugType.IMU;

        public enum state_ : uint
        {
            NOT_ATTACHED,
            DROPPED,
            NO_JOYCONS,
            ATTACHED,
            INPUT_MODE_0x30,
            IMU_DATA_OK,
        };

        public state_ state;

        private const uint report_len = 49;
        byte[] default_buf = { 0x0, 0x1, 0x40, 0x40, 0x0, 0x1, 0x40, 0x40 };
        private byte global_count = 0;
        public bool imu_enabled = false;
        private Rumble rumble_obj;
        private bool stop_polling;

        public NintendoJoyCon(IButtplugLogManager aLogManager, IHidDevice aHid, NintendoJoyConHidDeviceInfo aDeviceInfo)
            : base(aLogManager, aHid, aDeviceInfo)
        {
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceMessageHandler(HandleStopDeviceCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceMessageHandler(HandleVibrateCmd));
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceMessageHandler(HandleSingleMotorVibrateCmd));
            rumble_obj = new Rumble(160, 320, 0);
            // TODO Why is this attaching in a constructor, where nothing can throw?!
            aHid.OpenDevice();
            Attach(3);
        }

        private Thread PollThreadObj;
        private void Poll()
        {
            int attempts = 0;
            while (!stop_polling && state > state_.NO_JOYCONS)
            {
                SendRumble(rumble_obj.GetData());
                if (rumble_obj.amp == 0)
                {
                    break;
                }

                // TODO deal with disconnection
            }

            stop_polling = false;
            DebugPrint("End poll loop.", DebugType.THREADING);
        }

        public void DebugPrint(string s, DebugType d)
        {
            if (debug_type == DebugType.NONE) { return; }
            if (d == DebugType.ALL || d == debug_type || debug_type == DebugType.ALL)
            {
                Console.WriteLine(s);
            }
        }

        protected byte[] Subcommand(byte sc, byte[] buf, uint len, bool print = true)
        {
            byte[] buf_ = new byte[report_len];
            Array.Copy(default_buf, 0, buf_, 2, 8);
            Array.Copy(buf, 0, buf_, 11, len);
            buf_[10] = sc;
            buf_[1] = global_count;
            buf_[0] = 0x1;
            if (global_count == 0xf)
            {
                global_count = 0;
            }
            else { ++global_count; }

            if (print) { PrintArray(buf_, DebugType.COMMS, len, 11, "Subcommand 0x" + string.Format("{0:X2}", sc) + " sent. Data: 0x{0:S}"); }

            // HIDapi.hid_write(handle, buf_, new UIntPtr(len + 11));
            WriteData(buf_);

            // int res = HIDapi.hid_read_timeout(handle, response, new UIntPtr(report_len), 50);
            var res = ReadData();
            if (res.Length < 1) DebugPrint("No response.", DebugType.COMMS);
            else if (print) { PrintArray(res, DebugType.COMMS, report_len - 1, 1, "Response ID 0x" + string.Format("{0:X2}", res[0]) + ". Data: 0x{0:S}"); }
            return res;
        }

        public void SetRumble(float low_freq, float high_freq, float amp, int time = 0)
        {
            if (rumble_obj.timed_rumble == false || rumble_obj.t < 0)
            {
                rumble_obj = new Rumble(low_freq, high_freq, amp, time);
            }
        }

        private void SendRumble(byte[] buf)
        {
            byte[] buf_ = new byte[report_len];
            buf_[0] = 0x10;
            buf_[1] = global_count;
            if (global_count == 0xf) { global_count = 0; }
            else { ++global_count; }
            Array.Copy(buf, 0, buf_, 2, 8);
            PrintArray(buf_, DebugType.RUMBLE, format: "Rumble data sent: {0:S}");
            WriteData(buf_);
        }

        protected void Attach(byte leds_ = 0x0)
        {
            state = state_.ATTACHED;
            byte[] a = { 0x0 };
            // Input report mode
            Subcommand(0x3, new byte[] { 0x3f }, 1, false);
            a[0] = 0x1;
            // dump_calibration_data();
            // Connect
            a[0] = 0x01;
            Subcommand(0x1, a, 1);
            a[0] = 0x02;
            Subcommand(0x1, a, 1);
            a[0] = 0x03;
            Subcommand(0x1, a, 1);
            a[0] = leds_;
            Subcommand(0x30, a, 1);
            Subcommand(0x40, new byte[] { (imu_enabled ? (byte)0x1 : (byte)0x0) }, 1, true);
            Subcommand(0x3, new byte[] { 0x30 }, 1, true);
            Subcommand(0x48, new byte[] { 0x1 }, 1, true);
            DebugPrint("Done with init.", DebugType.COMMS);
        }

        private void PrintArray<T>(T[] arr, DebugType d = DebugType.NONE, uint len = 0, uint start = 0, string format = "{0:S}")
        {
            if (d != debug_type && debug_type != DebugType.ALL) { return; }
            if (len == 0) { len = (uint)arr.Length; }
            string tostr = "";
            for (int i = 0; i < len; ++i)
            {
                tostr += string.Format((arr[0] is byte) ? "{0:X2} " : ((arr[0] is float) ? "{0:F} " : "{0:D} "), arr[i + start]);
            }
            DebugPrint(string.Format(format, tostr), d);
        }

        protected override bool HandleData(byte[] data)
        {
            BpLogger.Trace("Cyclone got data: " + BitConverter.ToString(data));
            return true;
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            stop_polling = true;
            SetRumble(160, 320, 0, 0);
            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            if (PollThreadObj == null)
            {
                PollThreadObj = new Thread(new ThreadStart(Poll));
                PollThreadObj.Start();
            }
            SetRumble(160, 320, (float)(aMsg as VibrateCmd).Speeds[0].Speed, 0);
            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            if (PollThreadObj == null)
            {
                PollThreadObj = new Thread(new ThreadStart(Poll));
                PollThreadObj.Start();
            }
            SetRumble(160, 320, (float)(aMsg as SingleMotorVibrateCmd).Speed, 0);
            return new Ok(aMsg.Id);
        }
    }
}