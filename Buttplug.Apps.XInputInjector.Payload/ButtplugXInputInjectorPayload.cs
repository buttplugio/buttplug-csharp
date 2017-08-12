using EasyHook;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Buttplug.Apps.XInputInjector.Interface;

namespace Buttplug.Apps.XInputInjector.Payload
{
    public class ButtplugXInputInjectorPayload : IEntryPoint
    {
        private readonly ButtplugXInputInjectorInterface _interface;
        private LocalHook _xinputSetStateHookObj;
        private Vibration _lastMessage = new Vibration {LeftMotorSpeed = 65535, RightMotorSpeed = 65535};
        private readonly Queue<Vibration> _messageQueue = new Queue<Vibration>();
        private static Exception _ex;
        private static ButtplugXInputInjectorPayload _instance;

        public ButtplugXInputInjectorPayload(
            RemoteHooking.IContext aInContext,
            String aInChannelName)
        {
            _interface = RemoteHooking.IpcConnectClient<ButtplugXInputInjectorInterface>(aInChannelName);
            _instance = this;
        }

        public void Run(
            RemoteHooking.IContext aInContext,
            String aInArg1)
        {
            _interface.Ping(RemoteHooking.GetCurrentProcessId(), "Payload installed. Running payload loop.");
            try
            {
                _xinputSetStateHookObj = LocalHook.Create(
                    LocalHook.GetProcAddress("xinput1_3.dll", "XInputSetState"),
                    new XInputSetStateDelegate(XInputSetStateHookFunc),
                    null);

                // Set hook for all threads.
                _xinputSetStateHookObj.ThreadACL.SetExclusiveACL(new Int32[1]);
            }
            catch (Exception e)
            {
                _interface.ReportError(RemoteHooking.GetCurrentProcessId(), e);
                return;
            }

            try
            {
                while (_interface.Ping(RemoteHooking.GetCurrentProcessId(), ""))
                {
                    Thread.Sleep(1);

                    if (_messageQueue.Count > 0)
                    {
                        lock (_messageQueue)
                        {
                            _interface.Report(RemoteHooking.GetCurrentProcessId(), _messageQueue);
                            _messageQueue.Clear();
                        }
                    }
                    if (_ex != null)
                    {
                        _interface.ReportError(RemoteHooking.GetCurrentProcessId(), _ex);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                _interface.ReportError(RemoteHooking.GetCurrentProcessId(), e);
            }
            _interface.Ping(RemoteHooking.GetCurrentProcessId(), "Exiting payload loop");
            _interface.Exit();
        }

        [DllImport("xinput1_3.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputSetState")]
        private static extern unsafe int XInputSetState(int arg0, void* arg1);

        private static unsafe int XInputSetStateShim(int aUserIndex, Vibration aVibrationRef)
        {
            return XInputSetState(aUserIndex, &aVibrationRef);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate uint XInputSetStateDelegate(int aGamePadIndex, ref Vibration aVibrationRef);

        private static uint XInputSetStateHookFunc(int aGamePadIndex, ref Vibration aVibrationRef)
        {
            try
            {
                // Always send to the controller first, then do what we need to.
                XInputSetStateShim(aGamePadIndex, aVibrationRef);

                ButtplugXInputInjectorPayload This = _instance;
                // No reason to send duplicate packets.
                if (This._lastMessage.LeftMotorSpeed == aVibrationRef.LeftMotorSpeed &&
                    This._lastMessage.RightMotorSpeed == aVibrationRef.RightMotorSpeed)
                {
                    return 0;
                }
                This._lastMessage = new Vibration {
                    LeftMotorSpeed = aVibrationRef.LeftMotorSpeed,
                    RightMotorSpeed = aVibrationRef.RightMotorSpeed
                };

                lock (This._messageQueue)
                {
                    This._messageQueue.Enqueue(This._lastMessage);
                }
            }
            catch (Exception e)
            {
                _ex = e;
            }

            return 0;
        }
    }

}