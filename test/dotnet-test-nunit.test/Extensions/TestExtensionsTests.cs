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
using System.Xml.Linq;
using Microsoft.Extensions.Testing.Abstractions;
using NUnit.Framework;
using NUnit.Runner.Extensions;

namespace NUnit.Runner.Test.Extensions
{
    [TestFixture]
    public class TestExtensionsTests
    {
        [Test]
        public void CanConvertIdToGuid()
        {
            var attr = new XAttribute("id", "10");
            var guid = attr.ConvertToGuid();
            Assert.That(guid, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void NullIdConvertsToEmpty()
        {
            XAttribute attr = null;
            var guid = attr.ConvertToGuid();
            Assert.That(guid, Is.EqualTo(Guid.Empty));
        }

        [Test]
        public void CanConvertToDateTime()
        {
            var attr = new XAttribute("start-time", "2016-05-26 19:43:09Z");
            var date = attr.ConvertToDateTime();
            Assert.That(date.Year, Is.EqualTo(2016));
            Assert.That(date.Month, Is.EqualTo(5));
            Assert.That(date.Day, Is.EqualTo(26));
            Assert.That(date.Hour, Is.EqualTo(19));
            Assert.That(date.Minute, Is.EqualTo(43));
            Assert.That(date.Second, Is.EqualTo(9));
            Assert.That(date.Offset.TotalHours, Is.EqualTo(0d));
        }

        [Test]
        public void NullDateTimeConvertsToNow()
        {
            XAttribute attr = null;
            var date = attr.ConvertToDateTime();
            var now = DateTime.UtcNow;
            Assert.That(date.Year, Is.EqualTo(now.Year));
            Assert.That(date.Month, Is.EqualTo(now.Month));
            Assert.That(date.Day, Is.EqualTo(now.Day));
            Assert.That(date.Hour, Is.EqualTo(now.Hour));
            Assert.That(date.Minute, Is.EqualTo(now.Minute));
        }

        [TestCase("1", 1d)]
        [TestCase("0.1", 0.1d)]
        [TestCase("0.001", 0.001d)]
        [TestCase("0.017533", 0.017533d)]
        [TestCase("0.0001", 0.001d, Description = "Minimum TimeSpan of 1 ms")]
        [TestCase("", 0.001d, Description = "Default TimeSpan of 1 ms")]
        public void CanConvertDurations(string duration, double expected)
        {
            var attr = new XAttribute("duration", duration);
            var ts = attr.ConvertToTimeSpan();
            Assert.That(ts.TotalSeconds, Is.EqualTo(expected).Within(0.001d));
        }

        [TestCase("Passed", TestOutcome.Passed)]
        [TestCase("Failed", TestOutcome.Failed)]
        [TestCase("Skipped", TestOutcome.Skipped)]
        [TestCase("Inconclusive", TestOutcome.Skipped)]
        [TestCase("", TestOutcome.None)]
        public void CanConvertTestOutcomes(string status, TestOutcome expected)
        {
            var attr = new XAttribute("status", status);
            var outcome = attr.ConvertToTestOutcome();
            Assert.That(outcome, Is.EqualTo(expected));
        }
    }
}
