// <copyright file="ButtplugConnectorJSONParser.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    // ReSharper disable once InconsistentNaming
    public class ButtplugConnectorJSONParser
    {
        /// <summary>
        /// Used for converting messages between JSON and Objects.
        /// </summary>
        [NotNull]
        private readonly ButtplugJsonMessageParser _parser = new ButtplugJsonMessageParser(new ButtplugLogManager());

        /// <summary>
        /// Converts a single <see cref="ButtplugMessage"/> into a JSON string.
        /// </summary>
        /// <param name="aMsg">Message to convert.</param>
        /// <returns>The JSON string representation of the message.</returns>
        public string Serialize(ButtplugMessage aMsg)
        {
            return _parser.Serialize(aMsg, ButtplugConsts.CurrentSpecVersion);
        }

        /// <summary>
        /// Converts an array of <see cref="ButtplugMessage"/> into a JSON string.
        /// </summary>
        /// <param name="aMsgs">An array of messages to convert.</param>
        /// <returns>The JSON string representation of the messages.</returns>
        public string Serialize(ButtplugMessage[] aMsgs)
        {
            return _parser.Serialize(aMsgs, ButtplugConsts.CurrentSpecVersion);
        }

        /// <summary>
        /// Converts a JSON string into an array of <see cref="ButtplugMessage"/>.
        /// </summary>
        /// <param name="aMsg">A JSON string representing one or more messages.</param>
        /// <returns>An array of <see cref="ButtplugMessage"/>.</returns>
        public IEnumerable<ButtplugMessage> Deserialize(string aMsg)
        {
            return _parser.Deserialize(aMsg);
        }
    }
}