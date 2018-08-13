// <copyright file="ConnectionEventArgs.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using JetBrains.Annotations;

namespace Buttplug.Server.Connectors.IPCServer
{
    public class IPCConnectionEventArgs
    {
        [NotNull]
        public string ClientName;

        public IPCConnectionEventArgs(string aClientName = "Unknown Client")
        {
            ClientName = aClientName;
        }
    }
}