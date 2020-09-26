# DEPRECATION WARNING

This project will deprecated and archived in the coming weeks/months, with the
C# implementation of buttplug moving to an FFI layer on top of buttplug-rs. Bugs
are currently being triaged from this library to buttplug-rs.

The new C# project will be at

[https://github.com/buttplugio/buttplug-rs-ffi/tree/master/csharp](https://github.com/buttplugio/buttplug-rs-ffi/tree/master/csharp)

The API will change, though minimally (mostly connection in methods), and we
will still distribute the package under the same nuget package ("buttplug").
Many of the support packages for connectors and device management will also be
deprecated, as their functionality will now be handled in Rust.

You may continue to use this repo for the time being, just wanted everyone to be
aware of the changes happening in the near future.

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

## Want to use your sex toys with Movie Players, Games, Etc?

If you're a user that just wants to use your sex toys with
pre-existing Buttplug software, check out [Intiface
Desktop](https://intiface.com/desktop).

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
- [Unity Game Engine Support](#unity-game-engine-support)
- [Third Party Applications Using Buttplug-C#](#third-party-applications-using-buttplug-c)
- [License](#license)

## Support The Project

If you find this project helpful, you can [support us via
Patreon](http://patreon.com/qdot) or [Github
Sponsors](https://github.com/sponsors/qdot)! Every donation helps us
afford more hardware to reverse, document, and write code for!

## Documentation

Documentation is available for the Buttplug Protocol and Frameworks,
as well as this specific library.

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

For a full list of supported devices, check out [IOSTIndex](https://iostindex.com/?filter0ButtplugSupport=1).

Operating System support is as follows:

- Windows 10 - Creators Update (15063, April 2017) or later
  - All Devices

- Windows 7/8 and 10 pre 15063
  - All USB/Serial/Other Devices (No Bluetooth)

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

## Unity Game Engine Support

Unity support is provided via a Unity Custom Package file, and is
available in the
[Buttplug Unity Repo](https://github.com/buttplugio/buttplug-unity).

## Applications Using Buttplug C#

- [Intiface Desktop](https://intiface.com/desktop) - Intiface Desktop sex toy server software
- [Intiface Game Haptics Router](https://intiface.com/ghr) - Reroute game vibration to sex toys
- [ScriptPlayer](https://github.com/FredTungsten/ScriptPlayer) - Native hardware synced movie player for Windows.

## License

Buttplug is BSD 3-Clause licensed. More information is available in
the LICENSE file.
