// ***********************************************************************
// Copyright (c) 2016 NUnit Project
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using Microsoft.Extensions.Testing.Abstractions;
using NUnit.Framework;
using NUnit.Runner.Sinks;
using MsTest = Microsoft.Extensions.Testing.Abstractions.Test;
using MsTestResult = Microsoft.Extensions.Testing.Abstractions.TestResult;

namespace NUnit.Runner.Test.Sinks
{
    public class RemoteTestExecutionSinkTests : BaseSinkTests
    {
        ITestExecutionSink _testSink;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _testSink = new RemoteTestExecutionSink(BinaryWriter);
        }

        [Test]
        public void SendTestStarted()
        {
            var test = CreateMockTest();
            _testSink.SendTestStarted(test);
            var message = GetMessage();
            Assert.That(message, Is.Not.Null);
            Assert.That(message.MessageType, Is.EqualTo(Messages.TestStarted));
            AssertAreEqual(test, GetPayload<MsTest>(message));
        }

        [Test]
        public void SendTestStartedThrowsWithNullTest()
        {
            Assert.That(() => _testSink.SendTestStarted(null), Throws.ArgumentNullException);
        }

        [Test]
        public void SendTestResult()
        {
            var testResult = CreateMockTestResult();
            _testSink.SendTestResult(testResult);
            var message = GetMessage();
            Assert.That(message, Is.Not.Null);
            Assert.That(message.MessageType, Is.EqualTo(Messages.TestResult));
            AssertAreEqual(testResult, GetPayload<MsTestResult>(message));
        }

        [Test]
        public void SendTestResultThrowsWithNullTestResult()
        {
            Assert.That(() => _testSink.SendTestResult(null), Throws.ArgumentNullException);
        }
    }
}
