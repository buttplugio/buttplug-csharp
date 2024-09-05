# Buttplug C# - Client Only Implementation

[![Patreon donate button](https://img.shields.io/badge/patreon-donate-yellow.svg)](https://www.patreon.com/qdot)
[![Github donate button](https://img.shields.io/badge/github-donate-ff69b4.svg)](https://www.github.com/sponsors/qdot)
[![Discourse Forums](https://img.shields.io/discourse/status?label=buttplug.io%20forums&server=https%3A%2F%2Fdiscuss.buttplug.io)](https://discuss.buttplug.io)
[![Discord](https://img.shields.io/discord/353303527587708932.svg?logo=discord)](https://discord.buttplug.io)
[![Twitter](https://img.shields.io/twitter/follow/buttplugio.svg?style=social&logo=twitter)](https://twitter.com/buttplugio)

<div align="center">
  <h3>
    <a href="https://buttplug-csharp.docs.buttplug.io">
      C# API Docs
    </a>
    <span> | </span>
    <a href="https://docs.buttplug.io/docs/spec">
      Protocol Spec
    </a>
    <span> | </span>
    <a href="https://docs.buttplug.io/docs/dev-guide">
      Developer Guide
    </a>
    </h3>
  </div>
  <div align="center">
  <h3>
    <a href="https://how.do.i.get.buttplug.in">
      User FAQ
    </a>
    <span> | </span>
    <a href="https://awesome.buttplug.io">
      Apps/Games Using Buttplug
    </a>
  </h3>
</div>

This repo houses are pure .Net version of the Buttplug C# Client, runnings the Version 3 Buttplug Spec. It is expected to run against either [Intiface Central (GUI)](https://intiface.com/central) or [Initface Engine (CLI)](https://github.com/intiface/intiface-engine). No Rust FFI bindings are required.

## What happened? Why is buttplug-csharp back?

For those of you that have been around a while, you may remember that this used to be the main C# implementation. From 2017 to 2020, it was the reference version of the Buttplug Intimate Haptics Control Standard. 

Then I rewrote everything in Rust because I like Rust more and it's far easier for me to port across platforms. This repo was archived ~2 years ago in preference to seating the C# client *and* server on top of the [Rust implementation of Buttplug](https://github.com/buttplugio/buttplug), as part of the [Rust FFI Project](https://github.com/buttplugio/buttplug-rs-ffi).

That project ended up being a partial failure.

While the FFI system is handy for languages where it is very difficult to rebuild some parts of the library (C/C++/Java/etc...), for managed langauges like Javascript and C#, requiring both the client and server to use the FFI was overkill, and caused many extremely difficult-to-debug issues. With that in mind, the C# implementation is being turned into a Client only, .Net native implementation that should run on all platforms.

**The embedded server/connector is no longer built for C#.** It's too difficult for me to keep the package up for all of the needed architectures, and debugging it was hell. For now, I'm recommending developers point their users at [Intiface Central](https://intiface.com/central) as a hub application. If you have a reason for needing an embedded server in C#, leave an issue on this repo and we can discuss.

## Documentation and Examples

API Documentation is available at [https://buttplug-csharp.docs.buttplug.io].

C# Usage Examples are available in the [Buttplug Developer Guide](https://docs.buttplug.io/docs/dev-guide), with source code and VS projects in the [Dev Guide Repo](https://github.com/buttplugio/docs.buttplug.io/tree/master/examples/csharp).

## Didn't ManagedButtplugIo already do this?

[ManagedButtplugIo](https://github.com/Er1807/ManagedButtplugIo/) is a community produced, .Net native version of the Buttplug C# API, modeled off the FFI (which itself was modeled off of the original C# API). It's been helping the community along while the C# FFI implementation went off a cliff.

They did a very good job and I'm very appreicative of it (and I've used it in some of my own projects)!

The main reason I'm still keeping up my own C# client is that this is a big part of a LOT of the systems I maintain, like the Unity plugin. As part of how I fund this project is consulting on those, I need an implementation I'm responsible for. That said, this doesn't have to be the *only* implementation, and I'll still be keeping all imeplementations listed both in this README and on our [Awesome List](https://awesome.buttplug.io)

## Nuget and Packages

As of v3.1.0, Buttplug is now a single package. Websocket connectors have been moved to System.Net.Websockets and are now included with the base library.

- [Buttplug](https://www.nuget.org/packages/Buttplug/) - The Client Implemenation. Only dependency
  is NewtonsonJSON and Microsoft.CSharp 4.7

## Contributing

If you have issues or feature requests, [please feel free to file an issue on this repo](issues/).

We are not looking for code contributions or pull requests at this time, and will not accept pull
requests that do not have a matching issue where the matter was previously discussed. Pull requests
should only be submitted after talking to [qdot](https://github.com/qdot) via issues on this repo
(or on [discourse](https://discuss.buttplug.io) or [discord](https://discord.buttplug.io) if you
would like to stay anonymous and out of recorded info on the repo) before submitting PRs. Random PRs
without matching issues and discussion are likely to be closed without merging. and receiving
approval to develop code based on an issue. Any random or non-issue pull requests will most likely
be closed without merging.

If you'd like to contribute in a non-technical way, we need money to keep up with supporting the
latest and greatest hardware. We have multiple ways to donate!

- [Patreon](https://patreon.com/qdot)
- [Github Sponsors](https://github.com/sponsors/qdot)
- [Ko-Fi](https://ko-fi.com/qdot76367)

## License

This project is BSD 3-Clause licensed.

```text

Copyright (c) 2016-2024, Nonpolynomial, LLC
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
```