// <copyright file="ButtplugMessageMetadata.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Buttplug.Core.Messages
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ButtplugMessageMetadata : Attribute
    {
        /// <summary>
        /// String name of the message. Can be similar between different spec versions of messages.
        /// </summary>
        public string Name { get; }

        public ButtplugMessageMetadata(string name)
        {
            Name = name;
        }
    }
}