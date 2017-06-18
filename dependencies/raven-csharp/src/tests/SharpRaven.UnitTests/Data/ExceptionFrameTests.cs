﻿#region License

// Copyright (c) 2016 The Sentry Team and individual contributors.
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
using System.Diagnostics;
using System.Reflection;

using NUnit.Framework;

using SharpRaven.Data;
using SharpRaven.UnitTests.Utilities;
using System.Linq;
using NSubstitute;

namespace SharpRaven.UnitTests.Data
{
    internal class StackFrameWithNullMethod : StackFrame
    {
        public override MethodBase GetMethod()
        {
            return null;
        }
    }

    [TestFixture]
    public class ExceptionFrameTests
    {

        [Test]
        public void Constructor_NullFrameMethod_DoesNotThrow()
        {
            // on some platforms (e.g. on mono), StackFrame.GetMethod() may return null
            // e.g. for this stack frame:
            //   at (wrapper dynamic-method) System.Object:lambda_method (System.Runtime.CompilerServices.Closure,object,object))

            var stackFrame = new StackFrameWithNullMethod();
            var frame = new ExceptionFrame(stackFrame);

            Assert.AreEqual("(unknown)", frame.Module);
            Assert.AreEqual("(unknown)", frame.Function);
            Assert.AreEqual("(unknown)", frame.Source);
        }

        [Test]
        public void Constructor_InAppFrames_Identified()
        {
            try
            {
                var nums = new[] { 5, 4, 3, 2, 1, 0 };
                nums.Select(it => 1 / it).ToList();
                // The line above will throw.
                Assert.Fail();
            }
            catch (Exception exception)
            {
                var frames = new StackTrace(exception, true).GetFrames();
                var sentryFrames = frames.Select(f => new ExceptionFrame(f)).ToList();

                // The first frame is InApp - (this function).
                Assert.True(sentryFrames.First().InApp);

                // The last frame is InApp (our lambda that divides by zero).
                Assert.True(sentryFrames.Last().InApp);

                // There must be at least one system (Linq) frame in the middle.
                Assert.Greater(sentryFrames.Count(it => !it.InApp), 0);
            }
        }

        [Test]
        public void Constructor_Preserves_Function_Names()
        {
            var stackTrace = new StackTrace(TestHelper.GetException());
            var stackFrame = stackTrace.GetFrames().Last();
            var frame = new ExceptionFrame(stackFrame);

            Assert.AreEqual(frame.Function, "GetException");
            Assert.AreEqual(frame.Module, "SharpRaven.UnitTests.Utilities.TestHelper");
        }

        private static ExceptionFrame MockStackFrame(string typeName, string methodName)
        {
            var type = Substitute.For<Type>();
            type.FullName.Returns(typeName);

            var method = Substitute.For<MethodInfo>();
            method.DeclaringType.Returns(type);
            method.Name.Returns(methodName);

            var stackFrame = Substitute.For<StackFrame>();
            stackFrame.GetMethod().Returns(method);

            return new ExceptionFrame(stackFrame);
        }

        [Test]
        public void Constructor_Demangles_Function_Names()
        {
            ExceptionFrame frame;

            frame = MockStackFrame("Flipdish.Web.Controllers.PollApiController+<RequestNewOrders>d__4", "MoveNext");
            Assert.AreEqual(frame.Function, "RequestNewOrders");
            Assert.AreEqual(frame.Module, "Flipdish.Web.Controllers.PollApiController");

            frame = MockStackFrame("Flipdish.Domain.DomainModel.Services.RestaurantService+<EditVrByJson>d__93", "MoveNext");
            Assert.AreEqual(frame.Function, "EditVrByJson");
            Assert.AreEqual(frame.Module, "Flipdish.Domain.DomainModel.Services.RestaurantService");
        }

        [Test]
        public void Contructor_Demangles_Lambdas()
        {
            var module = "Example.Module";

            Assert.AreEqual("BeginInvokeAsynchronousActionMethod { <lambda> }", MockStackFrame(module, "<BeginInvokeAsynchronousActionMethod>b__36").Function);
            Assert.AreEqual("InvokeActionMethodFilterAsynchronouslyRecursive { <lambda> }", MockStackFrame(module, "<InvokeActionMethodFilterAsynchronouslyRecursive>b__3d").Function);
            Assert.AreEqual("BeginInvokeAction { <lambda> }", MockStackFrame(module, "<BeginInvokeAction>b__1e").Function);

        }
    }
}
