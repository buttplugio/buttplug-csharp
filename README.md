# Buttplug - Windows C# Server Implementation

[![Build status](https://ci.appveyor.com/api/projects/status/kcrobh9kvkpcjlqg?svg=true)](https://ci.appveyor.com/project/qdot/buttplug-csharp)
[![codecov](https://codecov.io/gh/buttplugio/buttplug-csharp/branch/master/graph/badge.svg)](https://codecov.io/gh/buttplugio/buttplug-csharp)

[![Patreon donate button](https://img.shields.io/badge/patreon-donate-yellow.svg)](https://www.patreon.com/qdot)
[![Discourse Forum](https://img.shields.io/badge/discourse-forum-blue.svg)](https://metafetish.club)
[![Discord](https://img.shields.io/discord/353303527587708932.svg?logo=discord)](https://discord.buttplug.io)
[![Twitter](https://img.shields.io/twitter/follow/buttplugio.svg?style=social&logo=twitter)](https://twitter.com/buttplugio)

Buttplug is a framework for hooking up hardware to interfaces, where
hardware usually means sex toys, but could honestly be just about
anything. It's basically a userland HID manager for things that may
not specifically be HID.

If you're looking for the installer for the Websocket Server, Game
Vibration Router, or Kiiroo Emulator, [visit our releases page.](https://github.com/buttplugio/buttplug-csharp/releases/)

## Table Of Contents

- [Support The Project](#support-the-project)
- [API Documentation](#api-documentation)
- [Buttplug Spec and Documentation](#buttplug-spec-and-documentation)
- [Hardware Support](#hardware-support)
- [Installation](#installation)
- [Development](#development)
  - [Special Installation Steps When Building Buttplug Applications](#special-installation-steps-when-building-buttplug-applications)
- [Third Party Applications Using Buttplug-C#](#third-party-applications-using-buttplug-c)
- [License]()

## Support The Project

If you find this project helpful, you can [support us via
Patreon](http://patreon.com/qdot)! Every donation helps us afford more
hardware to reverse, document, and write code for!

## API Documentation

API documentation for the current release is available at
[https://buttplug-csharp.docs.buttplug.io](https://buttplug-csharp.docs.buttplug.io).

## Buttplug Spec and Documentation

Buttplug implementations are available in multiple languages (rust,
javascript, etc)/frameworks/platforms. For a full
list of documentation, libraries, and applications,
[see the README in the main buttplug repo](http://github.com/buttplugio/buttplug).

## Hardware Support

Buttplug C# Supports the following platforms:

- Windows 10 - Creators Update (15063, April 2017) or later
  - BLE Devices
    - Fleshlight Launch
    - Kiiroo Toys (Onyx, Pearl, Onyx 2, Pearl 2)
    - LiBo Whale
    - Lovense Toys
    - Magic Motion Toys
    - Mysteryvibe Crescendo
    - OhMiBod Fuse
    - Vibratissimo Toys
    - Vorze Interactive A10 Cyclone SA
    - Vorze UFO SA
    - WeVibe Toys
    - Youcups Warrior II
  - USB Devices
    - Rez Trancevibrator
    - Vorze Cyclone X10
  - Serial Devices
    - ErosTek ET312B
  - Other Devices
    - Xbox Compatible Gamepads (XInput, Vibration Control Only)
  
- Windows 7/8 and 10 pre 15063
  - Serial Devices
    - ErosTek ET312B
  - USB Devices
    - Rez Trancevibrator
    - Vorze Cyclone X10
  - Other Devices
    - Xbox Compatible Gamepads (XInput, Vibration Control Only)

Check [https://buttplug.io](https://buttplug.io) for a list a fully
up-to-date list of supported hardware, as well as planned hardware
support.

## Installation

### Libraries for Development

Packages and libraries from the buttplug-csharp repo are available via
[nuget](http://nuget.org). Simply run a search for "buttplug", or
[follow this link to the nuget "buttplug" search](https://www.nuget.org/packages?q=buttplug).

### Client Software

The Buttplug C# Repo hosts the Buttplug C# Development Libraries, as
well as the following applications, known as the Buttplug Application Suite:

- Buttplug Server
- Game Vibration Router

To download the installer for these applications, [visit our releases page.](https://github.com/buttplugio/buttplug-csharp/releases/)

### Special Installation Steps When Building Buttplug Applications

Due to a bug in the Windows 15063 SDK, applications using the BLE APIs
need to have an AppId in the registry with a special SDDL access
string. For more info on this bug,
see
[this msdn forums post](https://social.msdn.microsoft.com/Forums/en-US/58da3fdb-a0e1-4161-8af3-778b6839f4e1/bluetooth-bluetoothledevicefromidasync-does-not-complete-on-10015063?forum=wdk#ef927009-676c-47bb-8201-8a80d2323a7f).

This bug has been fixed as of Windows 10 Build 16053, so this note is
only relevant if you have users still running on Windows 10 Build
15063 or earlier. To get the build number of a Windows installation,
run the "winver" command.

To fix this issue, we add a registry key in the installer for the
Buttplug Application Suite. Check out the buttplug-installer.ino file
to see how it works.

## Third Party Applications Using Buttplug C#

- [ScriptPlayer](https://github.com/FredTungsten/ScriptPlayer) - Native hardware synced movie player for Windows.
- [Caveman BIOS Teaches Typing](https://curiousjp.itch.io/caveman-bios-teaches-erotic-typing) - Typing Training Game

## License

Buttplug is BSD 3-Clause licensed. More information is available in
the LICENSE file.
