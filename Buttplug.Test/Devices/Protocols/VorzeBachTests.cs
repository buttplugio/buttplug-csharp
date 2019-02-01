// <copyright file="VorzeCycloneSATests.cs" company="Nonpolynomial Labs LLC">
//     Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
//     Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
//     license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Buttplug.Devices.Protocols;
using NUnit.Framework;

namespace Buttplug.Test.Devices.Protocols
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class VorzeBachTests : VorzeSATests
    {
        private string _deviceName = "Bach smart";
        private byte _commandPrefix = 0x06;
        private VorzeSAProtocol.CommandType _commandType = VorzeSAProtocol.CommandType.Vibrate;

        [Test]
        public async Task TestAllowedMessages()
        {
            await TestAllowedMessages(_deviceName, _commandType);
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests

        [Test]
        public async Task TestStopDeviceCmd()
        {
            await TestStopDeviceCmd(_deviceName, _commandPrefix, _commandType);
        }

        [Test]
        public async Task TestSingleMotorVibrateCmd()
        {
            await TestSingleMotorVibrateCmd(_deviceName, _commandPrefix);
        }

        [Test]
        public async Task TestVibrateCmd()
        {
            await TestVibrateCmd(_deviceName, _commandPrefix);
        }

        [Test]
        public async Task TestInvalidCmds()
        {
            await TestInvalidCmds(_deviceName, _commandType);
        }
    }
}