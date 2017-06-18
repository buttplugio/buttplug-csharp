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

using System;

using NUnit.Framework;

using SharpRaven.Data;

namespace SharpRaven.UnitTests.Data
{
    [TestFixture]
    public class BreadcrumbsRecordTests {
        
        [Test]
        public void Constructor_BreadcrumbsRecord_Category_With_Log()
        {
            var breadcrumbsRecord = new Breadcrumb("log");

            Assert.That(breadcrumbsRecord.Category, Is.EqualTo("log"));
        }

        [Test]
        public void Constructor_BreadcrumbsRecord_WithType_Category_With_Log()
        {
            var breadcrumbsRecord = new Breadcrumb("log", BreadcrumbType.Navigation);

            Assert.That(breadcrumbsRecord.Category, Is.EqualTo("log"));
        }

        [Test]
        public void Constructor_BreadcrumbsRecord_TimestampNow()
        {
            var now = DateTime.UtcNow;

            var breadcrumbsRecord = new Breadcrumb("foo");

            Assert.That(breadcrumbsRecord.Timestamp, Is.GreaterThanOrEqualTo(now));
        }

        [Test]
        public void Constructor_BreadcrumbsRecord_WithType_TimestampNow()
        {
            var now = DateTime.UtcNow;

            var breadcrumbsRecord = new Breadcrumb("foo", BreadcrumbType.Navigation);

            Assert.That(breadcrumbsRecord.Timestamp, Is.GreaterThanOrEqualTo(now));
        }

        [TestCase(BreadcrumbType.Navigation)]
        [TestCase(BreadcrumbType.Http)]
        public void Constructor_BreadcrumbsRecord_WithCategories(BreadcrumbType type)
        {
            var breadcrumbsRecord = new Breadcrumb("foo", type);

            Assert.That(breadcrumbsRecord.Type, Is.EqualTo(type));
        }

        [Test]
        public void Constructor_BreadcrumbsRecord_TypeNull()
        {
            var breadcrumbsRecord = new Breadcrumb("foo");

            Assert.That(breadcrumbsRecord.Type, Is.Null);
        }

        [TestCase("foo message")]
        [TestCase("foo message   ")]
        [TestCase("  foo message")]
        [TestCase("  foo message   ")]
        public void Should_Retain_Espaces_in_Message(string message)
        {
            var breadcrumbsRecord = new Breadcrumb("foo") { Message = message };

            Assert.That(breadcrumbsRecord.Message, Is.EqualTo(message));
        }

    }
}