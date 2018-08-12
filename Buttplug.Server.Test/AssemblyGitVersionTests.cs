// <copyright file="AssemblyGitVersionTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using Buttplug.Core;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class AssemblyGitVersionTests
    {
        [Test]
        public void AssemblyGitVersionTest1()
        {
            var gv = new AssemblyGitVersion();
            Assert.AreEqual(string.Empty, gv.Value);
        }

        [Test]
        public void AssemblyGitVersionTest2()
        {
            var gv = new AssemblyGitVersion("foo");
            Assert.AreEqual("foo", gv.Value);
        }
    }
}
