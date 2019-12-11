# 0.5.6 (2019-12-10)

## Features

- New hardware support
  - Kiiroo Pearl 2, OhMiBod Esca 2
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
