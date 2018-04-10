using System;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    public interface IButtplugClient
    {
        /// <summary>
        /// Event fired on Buttplug device added, either after connect or while scanning for devices.
        /// </summary>
        [CanBeNull]
        event EventHandler<DeviceEventArgs> DeviceAdded;

        /// <summary>
        /// Event fired on Buttplug device removed. Can fire at any time after device connection.
        /// </summary>
        [CanBeNull]
        event EventHandler<DeviceEventArgs> DeviceRemoved;

        /// <summary>
        /// Event fired when the server has finished scanning for devices.
        /// </summary>
        [CanBeNull]
        event EventHandler<ScanningFinishedEventArgs> ScanningFinished;

        /// <summary>
        /// Event fired when an error has been encountered. This may be internal client exceptions or
        /// Error messages from the server.
        /// </summary>
        [CanBeNull]
        event EventHandler<ErrorEventArgs> ErrorReceived;

        /// <summary>
        /// Event fired when the client receives a Log message. Should only fire if the client has
        /// requested that log messages be sent.
        /// </summary>
        [CanBeNull]
        event EventHandler<LogEventArgs> Log;

        /// <summary>
        /// Gets the connected Buttplug devices
        /// </summary>
        ButtplugClientDevice[] Devices { get; }

        /// <summary>
        /// Is the client connected?
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Creates the connection to the Buttplug Server and performs the protocol hanshake.
        /// Once the WebSocket connetion is open, the RequestServerInfo message is sent;
        /// the response is used to set up the ping timer loop. The RequestDeviceList
        /// message is also sent here so that any devices the server is already connected to
        /// are made known to the client.
        ///
        /// <b>Important:</b> Ensure that <see cref="ButtplugWSClient.DeviceAdded"/>, <see cref="ButtplugWSClient.DeviceRemoved"/>
        /// and <see cref="ButtplugWSClient.ErrorReceived"/> handlers are set before Connect is called.
        /// </summary>
        /// <param name="aURL">The URL for the Buttplug WebSocket Server. This will likely be in the form wss://localhost:12345 (wss:// is to ws:// as https:// is to http://)</param>
        /// <param name="aIgnoreSSLErrors">When using SSL (wss://), this option prevents bad certificates from causing connection failures</param>
        /// <returns>An untyped Task; the await/async equivelent of void</returns>
        Task Connect(Uri aURL, bool aIgnoreSSLErrors = false);

        /// <summary>
        /// Closes the WebSocket Connection.
        /// </summary>
        /// <returns>An untyped Task; the await/async equivelent of void</returns>
        Task Disconnect();

        /// <summary>
        /// Instructs the server to start scanning for devices.
        /// New devices will be rasied as events to <see cref="ButtplugWSClient.DeviceAdded"/>.
        /// When scanning complets, an event will be sent to <see cref="ButtplugWSClient.ScanningFinished"/>.
        /// </summary>
        /// <returns>True if successful.</returns>
        Task<bool> StartScanning();

        /// <summary>
        /// Instructs the server to stop scanning for devices.
        /// If scanning was in progress, an event will be sent to <see cref="ButtplugWSClient.ScanningFinished"/> when the device managers have all stopped scanning.
        /// </summary>
        /// <returns>True if the server successful recieved the command. If there are errors when stoppong the device managers, events may be sent to <see cref="ButtplugWSClient.ErrorReceived"/></returns>
        Task<bool> StopScanning();

        /// <summary>
        /// Instructs the server to start forwarding log entries to the cleintf.
        /// Log entries will be rasied as events to <see cref="ButtplugWSClient.Log"/>.
        /// </summary>
        /// <param name="aLogLevel">The level of most detailed logs to send.</param>
        /// <returns>True if successful.</returns>
        Task<bool> RequestLog(string aLogLevel);

        /// <summary>
        /// Sends a DeviceMessage (e.g. <see cref="VibrateCmd"/> or <see cref="LinearCmd"/>)
        /// </summary>
        /// <param name="aDevice">The device to be controlled by the message</param>
        /// <param name="aDeviceMsg">The device message (Id and DeviceIndex will be overriden)</param>
        /// <returns>True if successful.</returns>
        Task<ButtplugMessage> SendDeviceMessage(ButtplugClientDevice aDevice, ButtplugDeviceMessage aDeviceMsg);
    }
}