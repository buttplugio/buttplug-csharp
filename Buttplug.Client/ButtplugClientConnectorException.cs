// <copyright file="ButtplugClientConnectorException.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Buttplug.Client
{
    public class ButtplugClientConnectorException : Exception
    {
        public ButtplugClientConnectorException()
        {
        }

        public ButtplugClientConnectorException(string message, Exception e = null)
           : base(message, e)
        {
        }
    }
}