# Buttplug C# - Client Only Implementation

[![Patreon donate button](https://img.shields.io/badge/patreon-donate-yellow.svg)](https://www.patreon.com/qdot)
[![Github donate button](https://img.shields.io/badge/github-donate-ff69b4.svg)](https://www.github.com/sponsors/qdot)
[![Discourse Forums](https://img.shields.io/discourse/status?label=buttplug.io%20forums&server=https%3A%2F%2Fdiscuss.buttplug.io)](https://discuss.buttplug.io)
[![Discord](https://img.shields.io/discord/353303527587708932.svg?logo=discord)](https://discord.buttplug.io)
[![Twitter](https://img.shields.io/twitter/follow/buttplugio.svg?style=social&logo=twitter)](https://twitter.com/buttplugio)

This repo houses are pure .Net version of the Buttplug C# Client, runnings the Version 3 Buttplug Spec. It is expected to run against either [Intiface Central (GUI)](https://intiface.com/central) or [Initface Engine (CLI)](https://github.com/intiface/intiface-engine). No Rust FFI bindings are required.

## What happened? Why is buttplug-csharp back?

For those of you that have been around a while, you may remember that this used to be the main C# implementation. From 2017 to 2020, it was the reference version of the Buttplug Intimate Haptics Control Standard. 

Then I rewrote everything in Rust because I like Rust more and it's far easier for me to port across platforms. This repo was archived ~2 years ago in preference to seating the C# client *and* server on top of the [Rust implementation of Buttplug](https://github.com/buttplugio/buttplug), as part of the [Rust FFI Project](https://github.com/buttplugio/buttplug-rs-ffi).

That project ended up being a partial failure.

While the FFI system is handy for languages where it is very difficult to rebuild some parts of the library (C/C++/Java/etc...), for managed langauges like Javascript and C#, requiring both the client and server to use the FFI was overkill, and caused many extremely difficult-to-debug issues. With that in mind, the C# implementation is being turned into a Client only, .Net native implementation that should run on all platforms.

## Documentation and Examples

Documentation is now available via the [Buttplug Developer Guide](https://docs.buttplug.io/docs/).

C# Examples are in the Dev Guide, and are available in the [Dev Guide Repo](https://github.com/buttplugio/docs.buttplug.io/tree/master/examples/csharp).

## Didn't ManagedButtplugIo already do this?

[ManagedButtplugIo](https://github.com/Er1807/ManagedButtplugIo/) is a community produced, .Net native version of the Buttplug C# API, modeled off the FFI (which itself was modeled off of the original C# API). It's been helping the community along while the C# FFI implementation went off a cliff.

They did a very good job and I'm very appreicative of it (and I've used it in some of my own projects)!

The main reason I'm still keeping up my own C# client is that this is a big part of a LOT of the systems I maintain, like the Unity plugin. As part of how I fund this project is consulting on those, I need an implementation I'm responsible for. That said, this doesn't have to be the *only* implementation, and I'll still be keeping all imeplementations listed both in this README and on our [Awesome List](https://awesome.buttplug.io)

## Nuget and Packages

In order to reduce the amount of required dependencies, Buttplug is currently split into 3 Nuget packages.

- [Buttplug](https://www.nuget.org/packages/Buttplug/) - The Client Implemenation. Only dependency
  is NewtonsonJSON and Microsoft.CSharp 4.7
- [Buttplug.Client.Connectors.WebsocketConnector](https://www.nuget.org/packages/Buttplug.Client.Connectors.WebsocketConnector/)
  - A Websocket connector built in top of
    [WebsocketListener](https://github.com/deniszykov/WebSocketListener). While a websocket
    implementation is very much needed to use the client libraries, This is kept as a seperate
    dependency in case developers want to use a connector built on top of another Websocket library
    that may work better with or is already integrated in their setup (Like WebsocketSharp).
- Buttplug.Util.WebsocketDevice (Not yet available) - This is a test package that allows simulation
  of devices via the Buttplug Websocket Device Manager. More info about this can be found in [The
  Buttplug Developer Guide](https://docs.buttplug.io/).

