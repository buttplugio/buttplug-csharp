// <copyright file="ET312HandshakeException.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

// ReSharper disable once CheckNamespace
namespace Buttplug.Server.Managers.SerialPortManager
{
    // ReSharper disable once InconsistentNaming
    public class ET312HandshakeException : Exception
    {
        public ET312HandshakeException(string message)
            : base(message)
        {
        }
    }
}
