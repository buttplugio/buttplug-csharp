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
