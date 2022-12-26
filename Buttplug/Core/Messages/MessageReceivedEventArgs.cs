// <copyright file="MessageReceivedEventArgs.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Event fired when a new <see cref="ButtplugMessage"/> is received.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Buttplug message that was received.
        /// </summary>
        public ButtplugMessage Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="aMessage">Buttplug message that was received.</param>
        public MessageReceivedEventArgs(ButtplugMessage aMessage)
        {
            Message = aMessage;
        }
    }
}