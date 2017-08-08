# Buttplug - Windows C# Server Implementation

[![Build status](https://ci.appveyor.com/api/projects/status/vf4qvxkp3o3p3we1/branch/master?svg=true)](https://ci.appveyor.com/project/qdot/buttplug-csharp/branch/master) [![codecov](https://codecov.io/gh/metafetish/buttplug-csharp/branch/master/graph/badge.svg)](https://codecov.io/gh/metafetish/buttplug-csharp) [![Patreon donate button](https://img.shields.io/badge/patreon-donate-yellow.svg)](https://www.patreon.com/qdot)

Buttplug is a framework for hooking up hardware to interfaces, where
hardware usually means sex toys, but could honestly be just about
anything. It's basically a userland HID manager for things that may
not specifically be HID.

In more concrete terms, think of Buttplug as something like
[osculator](http://www.osculator.net/) or [VRPN](http://vrpn.org), but
for sex toys. Instead of wiimotes and control surfaces, we interface
with vibrators, electrostim equipment, fucking machines, and other
hardware that can communicate with computers.

The core of buttplug works as a router. It is an application that
connects to driver libraries, to register and communicate with
different hardware. Clients can then connect over different means
(network, websockets, etc...), to interact with the hardware.

## Other Buttplug Implementations

Buttplug implementations are available in multiple languages (rust,
javascript, etc)/frameworks/platforms. For a full
list,
[see the README in the main buttplug repo](http://github.com/metafetish/buttplug).

## Platform Support

Buttplug C# Supports the following platforms:

- Windows 10 - Creators Update (15063)
  - BLE Devices: Fleshlight Launch, Lovense Toys, Vorze Interactive,
    Vibratissimo, Magic Motion
  - Other devices: Xbox Gamepads (XInput)
  
- Windows 7/8 and 10 pre 15063
  - Other devices: Xbox Gamepads (XInput)

## Development

Packages and libraries from the buttplug-csharp repo are available
via [nuget](http://nuget.org). Simply run a search for "buttplug",
or
[follow this link to the nuget "buttplug" search](https://www.nuget.org/packages?q=buttplug).

## Special Installation Steps

Due to a bug in the Windows 15063 SDK, applications using the BLE APIs
need to have an AppId in the registry with a special SDDL access
string. For more info on this bug,
see
[this msdn forums post](https://social.msdn.microsoft.com/Forums/en-US/58da3fdb-a0e1-4161-8af3-778b6839f4e1/bluetooth-bluetoothledevicefromidasync-does-not-complete-on-10015063?forum=wdk#ef927009-676c-47bb-8201-8a80d2323a7f).

For those using the installer, this step should be taken care of by
InnoSetup. If you are building the project locally, start regedit,
then choose File > Import, and select the app.reg file in this repo.
This will set permissions for all executables named Buttplug.exe,
ButtplugCLI.exe, and ButtplugGUI.exe

## Support The Project

If you find this project helpful, you
can
[support Metafetish projects via Patreon](http://patreon.com/qdot)!
Every donation helps us afford more hardware to reverse, document, and
write code for!

## License

Buttplug is BSD licensed.

    Copyright (c) 2016-2017, Metafetish
    All rights reserved.
    
    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are met:
    
    * Redistributions of source code must retain the above copyright notice, this
      list of conditions and the following disclaimer.
    
    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and/or other materials provided with the distribution.
    
    * Neither the name of buttplug nor the names of its
      contributors may be used to endorse or promote products derived from
      this software without specific prior written permission.
    
    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
    AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
    FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
    DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
    SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
    CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
    OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
    OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
