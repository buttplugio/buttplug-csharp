// <copyright file="GlobalAssemblyInfo.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Reflection;

[assembly: AssemblyProduct("Buttplug")]
[assembly: AssemblyCompany("Nonpolynomial Labs LLC")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCopyright("Copyright Nonpolynomial Labs LLC 2017-2018")]

[assembly: AssemblyVersion(ThisAssembly.Git.BaseTag)]
[assembly: AssemblyFileVersion(ThisAssembly.Git.BaseTag)]
[assembly: AssemblyInformationalVersion(ThisAssembly.Git.Tag)]
[assembly: AssemblyMetadata("GitVersion", ThisAssembly.Git.Tag)]