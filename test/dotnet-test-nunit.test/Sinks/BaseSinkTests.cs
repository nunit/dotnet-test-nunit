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

using System;
using System.IO;
using Microsoft.Extensions.Testing.Abstractions;
using Newtonsoft.Json;
using NUnit.Framework;
using MsTest = Microsoft.Extensions.Testing.Abstractions.Test;
using MsTestResult = Microsoft.Extensions.Testing.Abstractions.TestResult;

namespace NUnit.Runner.Test.Sinks
{
    [TestFixture]
    public class BaseSinkTests
    {
        MemoryStream _stream;

        protected BinaryWriter BinaryWriter { get; private set; }

        [SetUp]
        public virtual void SetUp()
        {
            _stream = new MemoryStream();
            BinaryWriter = new BinaryWriter(_stream);
        }

        [TearDown]
        public virtual void TearDown()
        {
            BinaryWriter.Dispose();
            _stream.Dispose();
        }

        protected Message GetMessage()
        {
            _stream.Position = 0;
            var reader = new BinaryReader(_stream);
            var json = reader.ReadString();
            return JsonConvert.DeserializeObject<Message>(json);
        }

        protected T GetPayload<T>(Message message)
        {
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Payload, Is.Not.Null);
            return message.Payload.ToObject<T>();
        }

        protected static MsTest CreateMockTest() =>
            new MsTest
            {
                Id = Guid.NewGuid(),
                DisplayName = "NUnitTest",
                FullyQualifiedName = "NUnit.Runner.Test.NUnitTest",
                CodeFilePath = @"C:\src\NUnitTest.cs",
                LineNumber = 123
            };

        protected static MsTestResult CreateMockTestResult()
        {
            var test = CreateMockTest();
            var endTime = DateTime.UtcNow;
            return new MsTestResult(test)
            {
                StartTime = endTime.AddSeconds(-2),
                EndTime = endTime,
                Duration = TimeSpan.FromSeconds(2),
                ComputerName = "MyComputer",
                ErrorMessage = "An error occured",
                ErrorStackTrace = "Stack trace",
                Outcome = TestOutcome.Failed
            };
        }

        protected static void AssertAreEqual(MsTest expected, MsTest actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.DisplayName, actual.DisplayName);
            Assert.AreEqual(expected.FullyQualifiedName, actual.FullyQualifiedName);
            Assert.AreEqual(expected.CodeFilePath, actual.CodeFilePath);
            Assert.AreEqual(expected.LineNumber, actual.LineNumber);
        }

        protected static void AssertAreEqual(MsTestResult expected, MsTestResult actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.AreEqual(expected.StartTime, actual.StartTime);
            Assert.AreEqual(expected.EndTime, actual.EndTime);
            Assert.AreEqual(expected.Duration, actual.Duration);
            Assert.AreEqual(expected.DisplayName, actual.DisplayName);
            Assert.AreEqual(expected.ComputerName, actual.ComputerName);
            Assert.AreEqual(expected.ErrorMessage, actual.ErrorMessage);
            Assert.AreEqual(expected.ErrorStackTrace, actual.ErrorStackTrace);
            Assert.AreEqual(expected.Outcome, actual.Outcome);
        }
    }
}
