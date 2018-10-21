// <copyright file="AssemblyGitVersionTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using Buttplug.Core;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class AssemblyGitVersionTests
    {
        [Test]
        public void AssemblyGitVersionTest1()
        {
            var gv = new AssemblyGitVersion();
            gv.Value.Should().BeEmpty();
        }

        [Test]
        public void AssemblyGitVersionTest2()
        {
            var gv = new AssemblyGitVersion("foo");
            gv.Value.Should().Be("foo");
        }
    }
}
