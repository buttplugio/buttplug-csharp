using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Timer = System.Timers.Timer;

namespace Buttplug.Server
{
    public abstract class TimedScanDeviceSubtypeManager : DeviceSubtypeManager
    {
        protected Timer _scanTimer = new Timer();
        protected SemaphoreSlim _scanLock = new SemaphoreSlim(1, 1);

        protected TimedScanDeviceSubtypeManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            _scanTimer.Enabled = false;

            // 2 seconds seems like an ok default time for the moment.
            _scanTimer.Interval = 2000;
            _scanTimer.Elapsed += OnScanTimer;
        }

        protected void OnScanTimer(object aObj, ElapsedEventArgs aArgs)
        {
            if (_scanLock.CurrentCount == 0)
            {
                return;
            }

            try
            {
                _scanLock.Wait();
                RunScan();
            }
            catch (Exception e)
            {
                BpLogger.Error($"Caught timed-repeat scanning exception (aborting further scans for {GetType().Name}).");
                BpLogger.Error($"Exception Message: ${e.Message}");
                _scanTimer.Enabled = false;
                InvokeScanningFinished();
            }
            finally
            {
                _scanLock.Release();
            }
        }

        protected abstract void RunScan();

        public override Task StartScanning()
        {
            _scanTimer.Enabled = true;
            BpLogger.Info($"Starting timed-repeat scanning for {GetType().Name}");
            return Task.CompletedTask;
        }

        public override Task StopScanning()
        {
            // todo We need to be able to kill a CancellationToken here, otherwise things like ET312 connects will stall here.
            _scanLock.Wait();
            _scanTimer.Enabled = false;
            _scanLock.Release();
            BpLogger.Info($"Stopping timed-repeat scanning for {GetType().Name}");
            InvokeScanningFinished();
            return Task.CompletedTask;
        }

        public override bool IsScanning()
        {
            return _scanTimer.Enabled;
        }
    }
}
