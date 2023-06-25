# 3.0.1 (2023-06-18)

## Bugfixes

- Readded StopAllDevicesAsync method
- Moved from using dataflow to channel for websocket connector
- Fixed bug with DeviceRemoved not exposing indexes correctly
- Add Unknown to Sensor Types
- Fix multiple Websocket related bugs (ping, async tasks, etc...)

# 3.0.0 (2022-12-30)

Welcome Back, Buttplug C#

## Features

- All Server Components stripped out, library is now Client Only, expected to be run against either
  Intiface Central or Intiface engine.
- Updated to the Buttplug v3 spec, including ScalarCmd and SensorReadCmd
- JsonSchema removed
- Reflection requirements removed
- IPC Connector removed (Rust implementation won't be using it either, so no reason to have it here)
- Examples removed (moved to dev guide)
- Removed .Net Framework support (no longer needed)
- Added Standard 2.1 support (Alongside Standard 2.0)
- Websocket Client Connector returns to being its own package.

# 2.0.5 (2021-08-29)

## Features

- Update to Buttplug v5
  - New version of btleplug
  - New comm managers and other features that need to be ported to FFI

# 2.0.4 (2021-08-21)

## Bugfixes

- #87: Update ButtplugClient to use ConcurrentDictionary for device storage

# 2.0.3 (2021-05-17)

## Bugfixes

- #78: Fix key duplication exception when > 1 client instances created.
- #79: Fix issue with re-entrancy causing double-disposes

# 2.0.2 (2021-05-15)

## Features

- #77: C# Cleanup/Linting

## Bugfixes

- #76: Fix issue with log messages causing exceptions on device drop in FFI

# 2.0.1 (2021-04-24)

## Bugfixes

- #69: Sorter now throws a ButtplugConnectorException (similar to disconnect errors) for tasks that
  are still live during shutdown.
- Update to buttplug-rs v3.0.3, fixing issue with RawWriteCmd JSON Schema

# 2.0.0 (2021-04-22)

## Features

- Update to buttplug-rs v3.0.2, using tokio runtimes and with better scoping for runtime
  setup/teardown
  - Mostly because it's the only way to get Buttplug Unity v1 working.
  - Also fixes some bugs with battery reading in Lovense and Magic Motion toys.

## Breaking Changes

- Log object signatures/names changed.

# 1.0.18 (2021-04-04)

## Bugfixes

- Update to buttplug-rs 2.1.9, fixes Lovense battery read issues, should reduce bluetooth disconnect
  panics on windows, cleans up some error log messages that aren't actually errors.

# 1.0.17 (2021-03-21)

## Features

- #53: Nuget package now contains both .net standard and .net framework 4.7 builds
- #37: Added IsScanning status to client
- Update to buttplug-rs 2.1.7, Lovense Desire Egg support, new btleplug version

## Bugfixes

- #58 / #61: Sorter callback now processes future results on C#/.Net executor, and catches errors on
  possible races.

# 1.0.16 (2021-02-20)

## Bugfixes

- Update to buttplug-rs v2.1.5, fixes issues with connection status races and some devices panicing
  on disconnect while running initialize()
- Fix issue in FFI where multiple reconnects on the same client can cause multiple events to be sent

# 1.0.15 (2021-02-15)

## Features

- Update to buttplug-rs v2.1.4
- Add hardware support
  - The Handy

## Bugfixes

- Fix issue with Lovense Serial Dongle timing
- Fix LoveAi Dolp compat

# 1.0.14 (2020-02-13)

## Bugfixes

- Update to buttplug-rs v2.1.3, fix issues with max speed causing errors/crashes

# 1.0.13 (2020-02-07)

## Bugfixes

- Update to buttplug-rs v2.1.2, fix issue where StopDeviceCmd may not work in in-process instances

# 1.0.12 (2020-02-06)

## Features

- Update to buttplug-rs v2.1.1, more error handling/bugfixes, Lovense/Nobra device handling

# 1.0.11 (2020-01-24)

## Bugfixes

- Update to buttplug-rs v2.0.5, fixes issues with XInput misaddressing, and DeviceMessageInfo
  serialization issues.

# 1.0.10 (2020-01-22)

## Bugfixes

- Update to buttplug-rs v2.0.3, fixing some message compat issues and restoring access to some
  stroking and rotating devices.

# 1.0.9 (2020-01-19)

## Features

- Update to buttplug-rs v2.0.2, hardware support for Lovense Ferri, lots of internal cleanup,
  lovense dongle fixes.

# 1.0.8 (2020-01-09)

## Features

- Update to buttplug-rs v1.0.5, with new hardware support for libo, prettylove, etc

## Bugfixes

- #47: Sending single commands should now trigger on all motors.

# 1.0.7 (2020-01-08)

## Bugfixes

- Fix issue with event emitters missing null conditionals, causing null throws if no handlers exist.

# 1.0.6 (2020-01-04)

## Bugfixes

- #45: Fix issue with disconnect/reconnect causing device index collisions.

# 1.0.5 (2020-01-02)

## Bugfixes

- Update to buttplug-rs v1.0.4 (via buttplug-rs-ffi v1.0.3), fixes XInput devices not emitting
  disconnected events.

# 1.0.4 (2021-01-01)

## Bugfixes

- Update to buttplug-rs v1.0.3 (via buttplug-rs-ffi v1.0.2, yay dependency trees), fixes btle device
  scanning issues, added XInput rescanning

# 1.0.3 (2020-12-31)

## Bugfixes

- Update to buttplug-rs v1.0.1, fixes device scanning race issue 

# 1.0.2 (2020-12-29)

## Bugfixes

- Hold a reference to the LogCallback Delegate for the duration of the process lifetime, otherwise
  we'll crash on callback setup for logging.

# 1.0.1 (2020-12-27)

## Features

- Update to buttplug-rs v1.0.1, with new device config format
- Expose new env logger handler

## Notes

- Due to a mishandled test version 2 years about in Buttplug C#, v1.0.0 is taken. Therefore we're
  moving straight to v1.0.1

# 1.0.0 Beta 8 (2020-12-20)

## Bugfixes

- Update to buttplug-rs 0.11.3 via ButtplugFFI v1b6. Fixes memory leaks and possible race condition
  issues.

# 1.0.0 Beta 7 (2020-12-13)

## Features

- Added Connected getter to client
- Added ability to request log level output, and emit logs via event.

# 1.0.0 Beta 6 (2020-12-11)

## Bugfixes

- Fixes emitting of ServerDisconnected and ScanningFinished events in client.

# 1.0.0 Beta 5 (2020-11-28)

## Bugfixes

- Change Build system to account for .Net Core or .Net Framework building. Currently only works on
  x64 builds, "Any CPU" will not work.

# 1.0.0 Beta 4 (2020-11-26)

## Features

- Added client ping method
- Added utility method for console logging

## Bugfixes

- StopDeviceCmd should now work on all devices, not just device with index 0
- Disconnect should now actually disconnect

# 1.0.0 Beta 3 (2020-11-23)

## Bugfixes

- Remove all Console log calls

## API Changes

- Suffix all Async methods with "Async"
- Make WebsocketConnector take a URI, not a string

# 1.0.0 Beta 2 (2020-11-22)

## API Changes

- Move back to a connector-object-like mechanism, using C# method overloading for the Connect call.
- Change main namespace to "Buttplug" to match older libraries. 

# 1.0.0 Beta 1 (2020-11-21)

## Features

- Initial nuget release of C# FFI API. Includes most basic functionality to run Buttplug.
- Contains very little code from Buttplug C# v0.x. Consider this a complete restart.

# 0.5.9 (2020-06-17)

## Bugfixes

- Move Unity package building from C# repo to its own repo

# 0.5.8 (2020-06-16)

## Features

- Added Unity Package to repo
- New hardware support
  - Vorze Piston SA

# 0.5.7 (2020-05-12)

## Features

- New hardware support
  - Kiiroo Onyx+

## Bugfixes

- Youou will now stop on StopDeviceCmd/StopAllDevices
- WeVibe Melt protocol fix

# 0.5.6 (2019-12-10)

## Features

- New hardware support
  - Kiiroo Pearl 2
  - OhMiBod Esca 2
- Merged Kiiroo support into single protocol, so Titan should work as
  a vibrator now.

# 0.5.5 (2019-12-06)

## Features

- New hardware support
  - Magic Motion Awaken, Equinox, Solstice

# 0.5.4 (2019-11-26)

## Features

- New hardware support
  - Lelo F1s
  - Aneros Vivi
  - Lovehoney Desire
  - Libo Sexy Fox, Lucy, Elle2
  - WeVibe Vector (for real this time. I think.)

# 0.5.3 (2019-10-03)

## Bugfixes

- Nothing actually in this release, just fixing a CI hiccup on 0.5.2

# 0.5.2 (2019-10-02)

## Bugfixes

- Websocket server should only send one JSON string per packet

# 0.5.1 (2019-09-26)

## Features

- Update Dependencies (including Plugin.BLE support for MacOS)
- New hardware support
  - Motorbunny Classic
  
# 0.5.0 (2019-07-28)

## Features

- Change device config file loading to use JSON, removing YAML library
  requirement
- New hardware support
  - WeVibe Vector
  - Kiiroo Clinoa
  - Realov Lydia, Irena
  - MagicMotion Vini, Fugu
  
# Bugfixes

- Fix UWP Gatt Service finding for some toys

# 0.4.7 (2019-06-30)

## Features

- Add Xamarin (iOS/Android) Bluetooth Manager
- New hardware support
  - Svakom Ella
  - Libo Carlos, Shark, Lina, Adel, Lily

## Bugfixes

- Make XInput scanning less spammy
- Fix invalid read issue with Lovense Device Identification
- Fix package versioning with GitInfo (DLLs now show version)
- Fix building from non-git-repo (zip archives)
- Fix CI PR builds failing to sign binaries

# 0.4.6 (2019-05-26)

## Features

- New hardware support
  - PrettyLove brand
  - MonsterPub (Libo)
  
## Bugfixes

- Fixed conflict issues with BouncyCastle between our library and the
  PEM reader we use. Removed self-signed cert generation from library
  to do this. If you need a cert, use intiface or generate one with
  OpenSSL or something. But as this is a thing that has nothing to do
  with sex toys, it's No Longer My Problem.

# 0.4.5 (2019-05-26)

## Features

- New hardware support
  - Kiiroo Onyx 2.1
  - RealTouch
  
## Bugfixes

- Various fixes for HID devices

# 0.4.4 (2019-05-21)

## Features

- New hardware support
  - Mysteryvibe Tenuto
- DeviceManager can now be accessed from a ButtplugServer via a getter
  
## Bugfixes

- Don't hold disconnected devices in the main device array (#618)
- Fix nuget display icons

# 0.4.3 (2019-03-31)

## Features

- Removed CLI, now at https://github.com/intiface/intiface-cli-csharp
- Nuget packages now cryptographically signed
- Update UWP Bluetooth Manager to Win10 17763 SDK
- Added support for more LiBo Devices

## Bugfixes

- Fixed issue with bluetooth devices not reconnecting (#602)
- Fixed redundant calls to AddAllSubtypeManagers (#610)

# 0.4.2 (2019-03-15)

## Features

- Readied CLI for Intiface
- Added PEM cert reading to Websocket Server

# 0.4.1 (2019-03-08)

## Features

- Mostly CI/Release work in preparation for Intiface Release
- Added Hardware Support
  - Youou Wand

## Bugfixes

- Fixed check for lack of BLE characteristics
- Fixed issue with BLE devices not being seen again after connecting once

# 0.4.0 (2019-02-12)

## Features

- Implemented external device configuration files and user device configuration files
- Moved device protocols into base library
- Rebuilt Device Subtype Managers to only handle device communications, not protocol specifics
- Move CI to Azure Pipelines
- Move all project files to MSBuild format
- CI now builds Windows/Mac/Linux on all builds
- Added CLI program, handles all server tasks
- Moved TestDevice/Manager classes into main library
- Switch to using HIDSharp for Serial/HID on all platforms
- Added Hardware Support
  - Lovelife Krush
  - Cueme Underwear
  
## Bugfixes

- Subtype Managers without continuous scanning will now poll for devices
- Corrected MagicMotion protocol speed scaling
- Devices no longer ignore stop commands if stop command is first command sent.

## Known Issues

- Erostek ET-312 support disabled due to deadlocks

# 0.3.3 (2019-01-13)

## Features

- Added Hardware Support
  - Vorze Bach
- Moved TestServer/DeviceManager/Device to Core library for examples
- Updated dependencies

# 0.3.2 (2018-11-23)

## Features

- Added Hardware Support
  - Picobong Toys (All)
  
## Bugfixes

- Updates schema to fix LinearCmd issue

# 0.3.1 (2018-11-08)

## Features

- Added Hardware Support
  - Lovense Osci
  - Magic Motion Magic Wand
  - Magic Motion Magic Kegel Twins
  
## Bugfixes

- Updated schema to fix NJsonSchema parsing issues
- Fixed issue with not being able to scan for devices more than once per session

# 0.3.0 (2018-11-03)

## Features

- Added Hardware Support
  - Kiiroo Titan
  - Kiiroo Onyx 1 (Bluetooth)
  - WeVibe Classic
- Made code more C#-y
  - Methods exposed to developers now throws exceptions on errors, versus returning Buttplug Messages
  - All async methods now end in Async, take cancellation tokens
- Complete rewrite of Client API
  - More closely resembles buttplug-js client API
  - Uses extensible Connector setup for managing communications
  - More ergonomic API for developers, should never have to form raw Buttplug Messages
  - Server now searches local DLLs for Subtype Managers, versus requiring client to specifically add them
- Nuget Core/Client/Server packages now condensed to Buttplug package
- Buttplug package and Client/Server connectors now .Net Standard/Framework, supporting on Linux/Mac as well as Windows
  - Hardware Subtype Manager libraries still .Net Framework, because they're windows specific
- Lovense devices now support LovenseCmd message
- IPC Connectors for Client/Server
- SerialPortManager now capable of using protocols other than ET312
- Remove GUI applications (now at https://github.com/buttplugio/buttplug-windows-suite)
- Kiiroo Emulator completely removed
- Added example/tutorial programs to show developers how Buttplug C# should work
- Lots of Documentation updates
- Added Copyright to (most) files
- Lots more tests
- Converted all tests to using FluentAssertions
- Print Bluetooth radio info on first scan when logging at Debug or higher
- Use Types over strings where possible

## Bugfixes

- Lovense legacy devices (using BLE Indicate) now detected via same method as newer devices
- Catch exception when serial port with incorrect name shows up
- Lots of Resharper warnings fixes
- Lots of spelling fixes

# 0.2.3 (2018-05-23)

## Bugfixes

- Fix issue with Launch not connecting
- Fix issue with WeVibe/Youcups devices not being found

# 0.2.2 (2018-05-21)

## Features

- Added Hardware Support
  - Vorze UFO SA
  - LiBo Whale
  - MysteryVibe Crescendo
  - Cyclone X10 (USB)
  - Kiiroo Onyx 2
- Added name prefix device searching (Hopefully fixes Lovense update problems)
- Rename WebsocketServer to Server in preparation for IPC
- Add signal multiplier to GVR, for games with light vibration
- Add controller passthru to GVR, to allow turning off gamepad rumble when routing to toys

## Bugfixes

- Remove ping checking from Server to stop background tab disconnects
  on webbrowsers
- Move all .Net Standard project to .Net 4.7
- Update dependencies
- Change server GUI from disappearing to disabling on server stop
- Clear last error on server on successful connect or server start
- Fix lockup when closing applications that use the device tab and have a device scan going
- Fix crash when device names is missing in friendly name tables
- Fix crash when trying to open link on systems without a browser selected.
- Fix crash when Crypto key can't be written to disk
- Fix crash when Trancevibrator registry lookup returns unexpected types

# 0.2.1 (2018-03-08)

## Features

- Added Hardware Support
  - Lovense Lush/Domi/Edge (new firmware versions)
  - WeVibe Sync
  - Kiiroo Pearl 2
  - Pornhub Blowbot
  - Rez Trancevibrator (Win 7 only)
- Game Vibration Router now have "Vibes" tab to show incoming vibration commands
- Added individual vibrator control for WeVibes

## Bugfixes

- Fixed XInput DLL missing crash
- Fixed BAD DATA error/crash on accepting certs
- Moved all Non .Net Standard projects to .Net 4.7
- Far more test coverage
- Game Vibration Router only updates toys at 20hz max

# 0.2.0 (2018-01-22)

## Features

- Added Hardware Support
  - Youcups Warrior II Masturbator
  - Erostek ET312B
  - Wevibe 4
  - OhMiBod/Kiiroo Fuse
  - Lovense Edge/Hush/Domi (new firmware versions)
  - Individual Vibrator support for Lovense Edge
- Now uses v1 of the Buttplug Protocol spec, adds new generic messages, as well as feature counts for device messages
- Supports message downgrading, meaning older clients can connect to newer servers
  - Newer clients cannot connect to older servers, though
- Moved code to .Net Standard 2.0 compatibility
- Moved testing to NUnit

## Bugfixes

- Game Router process select button disabled until process selected
- Fix SynchronizationContext crash in client

# 0.1.3 (2017-12-07)

## Features

- Add ignore cert errors for client library
- Handle message ID iteration in client library

# 0.1.2 (2017-09-15)

## Bugfixes

- Fix ArgumentOutOfBoundsException in updater

# 0.1.1 (2017-09-15)

## Features

- Added auto update and update checking functionality
- Added support for the following hardware
  - WeVibe 4 Plus, Ditto, Nova, Pivot, Wish, Verge
  - Lovense Domi
- Added more product names for the Lovense Hush (LVS-Z36, LVS_Z001)
- Added Game Vibration Router application
- WebsocketServer now defaults to SSL

## Bugfixes

- Fixed hang when no XBox controllers and no Bluetooth adapters are
  connected
- SSL Errors in Websocket Server are now shown in GUI or as a
  notification, not in modal dialogs
- Fixed ObjectDisposed Exception in Kiiroo App
- Fixed port number changing in Websocket Server
- Fixed crash when copying IP addresses in Websocket Server
- Fixed version number listing in logs
- Vibratissimo devices now required to be named "Vibratissimo"

# 0.1.0 (2017-08-07)

## Features

- First release
- Added support for the following hardware
  - XInput (XBox) Gamepads
  - Lovense Max, Nora, Lush, Hush, Ambi, Edge (vibration only)
  - Fleshlight Launch
  - Vorze A10 Cyclone
  - Magic Motion toys
  - Vibratissimo toys
- Added libraries (available as nuget packages)
  - Core
  - Client
  - Server
  - XInputGamepadManager
  - UWPBluetoothManager
- Added applications
  - Websocket Server
  - Kiiroo Platform Emulator

# 0.0.0 (2017-04-24)

- Project Started
