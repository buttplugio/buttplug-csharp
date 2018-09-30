// <copyright file="ET312CommunicationException.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Buttplug.Server.Managers.SerialPortManager
{
    // ReSharper disable once InconsistentNaming
    public class ErostekET312CommunicationException : Exception
    {
        public ErostekET312CommunicationException(string message)
            : base(message)
        {
        }
    }
}
