# Buttplug Protocol Implementation - C# .Net Standard/Framework

[![Build Status](https://dev.azure.com/nplabs/buttplug/_apis/build/status/buttplugio.buttplug-csharp?branchName=master)](https://dev.azure.com/nplabs/buttplug/_build/latest?definitionId=2&branchName=master)
[![codecov](https://codecov.io/gh/buttplugio/buttplug-csharp/branch/master/graph/badge.svg)](https://codecov.io/gh/buttplugio/buttplug-csharp)
[![NuGet](https://img.shields.io/nuget/v/Buttplug.svg)](https://www.nuget.org/packages/Buttplug/)

[![Patreon donate button](https://img.shields.io/badge/patreon-donate-yellow.svg)](https://www.patreon.com/qdot)
[![Discourse Forum](https://img.shields.io/badge/discourse-forum-blue.svg)](https://metafetish.club)
[![Discord](https://img.shields.io/discord/353303527587708932.svg?logo=discord)](https://discord.buttplug.io)
[![Twitter](https://img.shields.io/twitter/follow/buttplugio.svg?style=social&logo=twitter)](https://twitter.com/buttplugio)

Buttplug is a framework for hooking up hardware to interfaces, where
hardware usually means sex toys, but could honestly be just about
anything. Think of it as a userland HID manager for things that may
not specifically be HID, but may go in your butt. 

Or other places too! We're not completely butt-centric, despite the
project name. And logo.

If you're looking for the installer for the Buttplug Server or Game
Vibration Router software, [visit the buttplug-windows-suite releases
page.](https://github.com/buttplugio/buttplug-windows-suite/releases/)

## Table Of Contents

- [Support The Project](#support-the-project)
- [Documentation](#documentation)
    - [Developer Guide](#developer-guide)
    - [Library Usage Examples](#library-usage-examples)
    - [Library API Documentation](#library-api-documentation)
    - [Buttplug Spec and Documentation](#buttplug-spec-and-documentation)
- [Hardware Support](#hardware-support)
- [Installation](#installation)
- [Development Branches](#development-branches)
- [Compiling](#compiling)
- [Client Software (Buttplug Server, Game Vibration Router)](#client-software)
- [Third Party Applications Using Buttplug-C#](#third-party-applications-using-buttplug-c)
- [License](#license)

## Support The Project

If you find this project helpful, you can [support us via
Patreon](http://patreon.com/qdot)! Every donation helps us afford more
hardware to reverse, document, and write code for!

## Documentation

Documentation is available for the Buttplug Protocol and Frameworks,
as well as this specific library.

### Developer Guide

The [Buttplug Developer
Guide](https://buttplug-developer-guide.docs.buttplug.io) is the best
introduction to how Buttplug works as a system. It provides an
architectural overview of the various libraries and development
strategies, as well as code examples in multiple languages.

[https://buttplug-developer-guide.docs.buttplug.io](https://buttplug-developer-guide.docs.buttplug.io)

### Library Usage Examples

Want to know what using the library looks like in code? The project
includes some heavily commented examples!

- [Part 1 - Embedded Connectors](https://github.com/buttplugio/buttplug-csharp/blob/master/Buttplug.Examples.01.EmbeddedClientSetup/Program.cs)
- [Part 2 - Remote Connectors](https://github.com/buttplugio/buttplug-csharp/blob/master/Buttplug.Examples.02.WebsocketClientSetup/Program.cs)
- [Part 3 - Connection Lifetimes and Ping Timers](https://github.com/buttplugio/buttplug-csharp/blob/master/Buttplug.Examples.03.ConnectionLifetimesAndPingTimers/Program.cs)
- [Part 4 - Device Enumeration](https://github.com/buttplugio/buttplug-csharp/blob/master/Buttplug.Examples.04.DeviceEnumeration/Program.cs)
- [Part 5 - Device Control](https://github.com/buttplugio/buttplug-csharp/blob/master/Buttplug.Examples.05.DeviceControl/Program.cs)
- [Part 6 - Logging and Error Handling](https://github.com/buttplugio/buttplug-csharp/blob/master/Buttplug.Examples.06.LoggingAndErrorHandling/Program.cs)
- [Part 7 - Full Example Program](https://github.com/buttplugio/buttplug-csharp/blob/master/Buttplug.Examples.07.FullProgram/Program.cs)

### Library API Documentation

API documentation for the current release is available at
[https://buttplug-csharp.docs.buttplug.io](https://buttplug-csharp.docs.buttplug.io).

API documentation for the current development branch is available at
[https://buttplug-csharp-dev.docs.buttplug.io](https://buttplug-csharp-dev.docs.buttplug.io).

### Buttplug Spec and Documentation

Buttplug implementations are available in multiple languages (rust,
javascript, etc)/frameworks/platforms. For a full list of
documentation, libraries, and applications, [see the buttplug.io
website](https://buttplug.io).

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

- Linux/Mac (Mono)
  - No hardware support currently

Check [https://buttplug.io](https://buttplug.io) for a list a fully
up-to-date list of supported hardware, as well as planned hardware
support.

## Installation

Packages and libraries from the buttplug-csharp repo are available via
[nuget](http://nuget.org). Simply run a search for "buttplug", or
[follow this link to the nuget "buttplug" search](https://www.nuget.org/packages?q=buttplug).

## Development Branches

There are 2 main branches:

- master - The current release code for the library
- dev - The current development code for the library

When submitting patches, they should be based on the current state of
the dev branch, and should use dev as their target. The master branch
is kept synced to release so our documentation matches expectations.

## Compiling

The project should compile with Visual Studio 2017, Visual Studio on
Mac (.Net Standard projects only), or Mono on linux (.Net Standard
projects only).

Note that for building using Visual Studio on Windows 7/8/10, the
project requires the [Windows 10
SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk)
to be installed.

Note that some of the Windows 10 SDK links in the UWP Bluetooth
Manager project are hard-linked to the C:\ drive. If your program
files and SDKs are not on your C: drive, you may need to readjust
these paths by hand.

## Client Software

Buttplug programs including

- Buttplug Server
- Game Vibration Router

have moved to the Buttplug Windows Suite Repo. To download the
installer for these applications, [visit the releases
page for that repo.](https://github.com/buttplugio/buttplug-windows-suite/releases/)

## Third Party Applications Using Buttplug C#

- [Buttplug Windows Suite](https://github.com/buttplugio/buttplug-windows-suite) - Buttplug Server and Buttplug Game Vibration Router
- [ScriptPlayer](https://github.com/FredTungsten/ScriptPlayer) - Native hardware synced movie player for Windows.
- [Caveman BIOS Teaches Typing](https://curiousjp.itch.io/caveman-bios-teaches-erotic-typing) - Typing Training Game

## License

Buttplug is BSD 3-Clause licensed. More information is available in
the LICENSE file.
