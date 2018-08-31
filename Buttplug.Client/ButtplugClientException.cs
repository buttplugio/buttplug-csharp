// <copyright file="ButtplugClientException.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    public class ButtplugClientException : Exception
    {
        public readonly ButtplugMessage ButtplugErrorMessage;

        public readonly string ButtplugMessage;

        public ButtplugClientException(ButtplugMessage aMsg)
        {
            ButtplugErrorMessage = aMsg;
        }

        public ButtplugClientException(string aMessage)
        {
            ButtplugMessage = aMessage;
        }
    }
}