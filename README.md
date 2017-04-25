# Buttplug - C# Server Implementation

[![Patreon donate button](https://img.shields.io/badge/patreon-donate-yellow.svg)](https://www.patreon.com/qdot)

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
different hardware. Clients can then connect over websockets or
network ports, to claim and interact with the hardware.

## C# Version

Buttplug is currently coming together in 3 languages simulaniously.
This may sound stupid/nuts, and that's because it is. However,
buttplug itself is fairly simple, so the goal here is to make the
hardware support as transparent as possible for the user/developer.

The current implementation projects are in:

- [C#](http://github.com/metafetish/buttplug-csharp), for Windows, Unity, etc...
- [Rust](http://github.com/metafetish/buttplug-rs), for all platforms,
  but currently requiring a LOT of yak shaving to get where we need to
  be with library
  support.
  [See this blog post for more info.](https://buttplug.io/blog/2017/03/26/the-yak-factory/)
- Javascript (no central repo yet), for platforms with WebBluetooth
  support, and developers that want node or cordova support.
  
Whether any of these will get done is a really good question.

## Support The Project

If you find this project helpful, you
can
[support Metafetish projects via Patreon](http://patreon.com/qdot)!
Every donation helps us afford more hardware to reverse, document, and
write code for!

## License

Buttplug is BSD licensed.

    Copyright (c) 2016, Metafetish
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
