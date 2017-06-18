﻿#region License

// Copyright (c) 2014 The Sentry Team and individual contributors.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted
// provided that the following conditions are met:
// 
//     1. Redistributions of source code must retain the above copyright notice, this list of
//        conditions and the following disclaimer.
// 
//     2. Redistributions in binary form must reproduce the above copyright notice, this list of
//        conditions and the following disclaimer in the documentation and/or other materials
//        provided with the distribution.
// 
//     3. Neither the name of the Sentry nor the names of its contributors may be used to
//        endorse or promote products derived from this software without specific prior written
//        permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
// ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Security.Principal;

using NSubstitute;

using NUnit.Framework;

using SharpRaven.Data;

namespace SharpRaven.UnitTests.Data
{
    [TestFixture]
    public class SentryUserTests
    {
        [Test]
        public void Constructor_AssignedPrincipal_UsernameIsSet()
        {
            const string username = "chuck.norris";
            var principal = Substitute.For<IPrincipal>();
            principal.Identity.Name.Returns(username);
            var user = new SentryUser(principal);

            Assert.That(user.Username, Is.EqualTo(username));
        }


        [Test]
        public void Constructor_NullPrincipal_DoesNotThrow()
        {
            var user = new SentryUser((IPrincipal)null);
            Assert.That(user.Username, Is.Null);
        }


        [Test]
        public void Constructor_NullUsername_DoesNotThrow()
        {
            var user = new SentryUser((string)null);
            Assert.That(user.Username, Is.Null);
        }
    }
}