# Buttplug C# - Client Only Implementation

[![Patreon donate button](https://img.shields.io/badge/patreon-donate-yellow.svg)](https://www.patreon.com/qdot)
[![Github donate button](https://img.shields.io/badge/github-donate-ff69b4.svg)](https://www.github.com/sponsors/qdot)
[![Discord](https://img.shields.io/discord/353303527587708932.svg?logo=discord)](https://discord.buttplug.io)
[![Twitter](https://img.shields.io/twitter/follow/buttplugio.svg?style=social&logo=twitter)](https://twitter.com/buttplugio)

This repo houses are pure .Net version of the Buttplug C# Client, runnings the Version 3 Buttplug Spec. It is expected to run against either [Intiface Central (GUI)](https://intiface.com/central) or [Initface Engine (CLI)](https://github.com/intiface/intiface-engine). No Rust FFI bindings are required.

## What happened? Why is buttplug-csharp back?

For those of you that have been around a while, you may remember that this used to be the main C# implementation. From 2017 to 2020, it was the reference version of the Buttplug Intimate Haptics Control Standard. 

Then I rewrote everything in Rust because I like Rust more and it's far easier for me to port across platforms. This repo was archived ~2 years ago in preference to seating the C# client *and* server on top of the [Rust implementation of Buttplug](https://github.com/buttplugio/buttplug), as part of the [Rust FFI Project](https://github.com/buttplugio/buttplug-rs-ffi).

That project ended up being a partial failure.

While the FFI system is handy for languages where it is very difficult to rebuild some parts of the library (C/C++/Java/etc...), for managed langauges like Javascript and C#, requiring both the client and server to use the FFI was overkill, and caused many extremely difficult to debug issues. With that in mind, the C# implementation is being turned into a Client only, .Net native implementation that should run on all platforms.

## Nuget and Packages

In order to reduce the amount of required dependencies, Buttplug is currently split into 3 Nuget packages.

- Buttplug - The Client Implemenation. Only dependency is NewtonsonJSON (And Microsoft.CSharp 4.7)
- Buttplug.Client.Connectors.WebsocketConnector - A Websocket connector built in top of
  [WebsocketListener](https://github.com/deniszykov/WebSocketListener). While a websocket
  implementation is very much needed to use the client libraries, This is kept as a seperate
  dependency in case developers want to use a connector built on top of another Websocket library
  that may work better with or is already integrated in their setup (Like WebsocketSharp).
- Buttplug.Util.WebsocketDevice - This is a test package that allows simulation of devices via the
  Buttplug Websocket Device Manager. More info about this can be found in [The Buttplug Developer Guide](https://docs.buttplug.io/).

