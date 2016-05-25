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

using System.IO;
using Microsoft.Extensions.Testing.Abstractions;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Runner.Sinks;

namespace NUnit.Runner.Test.Sinks
{
    [TestFixture]
    public class RemoteTestDiscoverySinkTests
    {
        MemoryStream _stream;
        BinaryWriter _writer;
        ITestDiscoverySink _testSink;

        [SetUp]
        public void SetUp()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
            _testSink = new RemoteTestDiscoverySink(_writer);
        }

        [TearDown]
        public void TearDown()
        {
            _writer.Dispose();
            _stream.Dispose();
        }

        [Test]
        public void SendTestCompleted()
        {
            _testSink.SendTestCompleted();
            var result = GetMessage();
            Assert.That(result, Is.Not.Null);
            Assert.That(result.MessageType, Is.EqualTo(Messages.TestCompleted));
        }

        [Test]
        public void SendWaitingCommand()
        {
            _testSink.SendWaitingCommand();
            var result = GetMessage();
            Assert.That(result, Is.Not.Null);
            Assert.That(result.MessageType, Is.EqualTo(Messages.WaitingCommand));
        }

        protected Message GetMessage()
        {
            _stream.Position = 0;
            var reader = new BinaryReader(_stream);
            var json = reader.ReadString();
            return JsonConvert.DeserializeObject<Message>(json);
        }
    }
}
