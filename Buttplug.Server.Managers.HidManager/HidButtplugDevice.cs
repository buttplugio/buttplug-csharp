// <copyright file="HidButtplugDevice.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using HidLibrary;

namespace Buttplug.Server.Managers.HidManager
{
    internal abstract class HidButtplugDevice : ButtplugDevice, IDisposable
    {
        private readonly IHidDevice _hid;

        private readonly CancellationTokenSource _tokenSource;

        public readonly IHidDeviceInfo DeviceInfo;

        public readonly IButtplugLogManager LogManager;

        private bool _disposed;

        private bool _reading;

        private Task _readerThread;

        protected HidButtplugDevice(IButtplugLogManager aLogManager, IHidDevice aHid, IHidDeviceInfo aDeviceInfo)
            : base(aLogManager, aDeviceInfo.Name, aHid.DevicePath)
        {
            LogManager = aLogManager;
            _hid = aHid;
            DeviceInfo = aDeviceInfo;

            _tokenSource = new CancellationTokenSource();
            _hid.Inserted += DeviceAttachedHandler;
            _hid.Removed += DeviceRemovedHandler;
        }

        public override void Disconnect()
        {
            _hid.CloseDevice();
            InvokeDeviceRemoved();
        }

        public void BeginRead()
        {
            if (_readerThread != null && _readerThread.Status == TaskStatus.Running)
            {
                return;
            }

            _readerThread = new Task(ReportReader, _tokenSource.Token, TaskCreationOptions.LongRunning);
            _readerThread.Start();
        }

        public void EndRead(bool close)
        {
            _reading = false;
            _readerThread?.Wait();
            _readerThread = null;
            _hid.MonitorDeviceEvents = false;
            if (close)
            {
                _hid.CloseDevice();
            }
        }

        public List<byte> ReadData()
        {
            if (_disposed || !_hid.IsOpen)
            {
                return null;
            }

            var data = _hid.Read(100);
            return data.Status != HidDeviceData.ReadStatus.Success ? null : data.Data.ToList();
        }

        public bool WriteData(byte[] aData)
        {
            if (_disposed || !_hid.IsOpen)
            {
                return false;
            }

            return _hid.Write(aData, 100);
        }

        private static void DeviceAttachedHandler()
        {
        }

        private void DeviceRemovedHandler()
        {
            _reading = false;
            InvokeDeviceRemoved();
        }

        private void ReportReader()
        {
            if (!_hid.IsOpen)
            {
                _hid.OpenDevice();
            }

            _reading = true;
            _hid.MonitorDeviceEvents = true;
            _hid.ReadReport(OnReport, 100);
        }

        private void OnReport(HidReport report)
        {
            if (!report.Exists || report.ReadStatus != HidDeviceData.ReadStatus.Success)
            {
                return;
            }

            if (HandleData(report.Data) && _reading)
            {
                _hid.ReadReport(OnReport);
            }
        }

        protected abstract bool HandleData(byte[] data);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool aDisposing)
        {
            if (_disposed)
            {
                return;
            }

            if (aDisposing)
            {
                _hid.CloseDevice();
                InvokeDeviceRemoved();
            }

            _disposed = true;
        }
    }
}