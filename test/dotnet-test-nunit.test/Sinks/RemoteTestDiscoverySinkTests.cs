// ***********************************************************************
// Copyright (c) 2016 Charlie Poole
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

namespace NUnit.Runner.Test.Sinks
{
    [TestFixture]
    public class RemoteTestDiscoverySinkTests : BaseSinkTests
    {
        ITestDiscoverySink _testSink;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _testSink = new RemoteTestDiscoverySink(BinaryWriter);
        }

        [Test]
        public void SendTestFound()
        {
            var test = CreateMockTest();
            _testSink.SendTestFound(test);
            var message = GetMessage();
            Assert.That(message, Is.Not.Null);
            Assert.That(message.MessageType, Is.EqualTo(Messages.TestFound));
            AssertAreEqual(test, GetPayload<MsTest>(message));
        }

        [Test]
        public void SendTestFoundThrowsWithNullTest()
        {
            Assert.That(() => _testSink.SendTestFound(null), Throws.ArgumentNullException);
        }
    }
}
