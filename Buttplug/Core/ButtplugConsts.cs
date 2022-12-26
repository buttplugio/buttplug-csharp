// <copyright file="ButtplugConsts.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Buttplug.Core
{
    /// <summary>
    /// Buttplug library constants.
    /// </summary>
    public static class ButtplugConsts
    {
        /// <summary>
        /// Default ID for server originated messages.
        /// </summary>
        public const uint SystemMsgId = 0;

        /// <summary>
        /// Default message ID for messages not originating from the server. In remote client/server
        /// environments, message IDs should be unique (usually monotonically increasing), to prevent
        /// responses from colliding.
        /// </summary>
        public const uint DefaultMsgId = 1;

        /// <summary>
        /// Spec version this Buttplug library is based on.
        /// </summary>
        public const uint CurrentSpecVersion = 1;
    }
}