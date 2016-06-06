﻿// ***********************************************************************
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Testing.Abstractions;
using NUnit.Framework;
using MsTest = Microsoft.Extensions.Testing.Abstractions.Test;

namespace NUnit.Runner.Test.TestListeners
{
    [TestFixture]
    public class TestExecutionListenerTests
    {
        const string ERROR_TEST_CASE_XML =
                "<test-case id=\"1018\" name=\"ErrorTest\" fullname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests.ErrorTest\" methodname=\"ErrorTest\" classname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests\" runstate=\"Runnable\" seed=\"1658039280\" result=\"Failed\" label=\"Error\" start-time=\"2016-06-06 19:57:35Z\" end-time=\"2016-06-06 19:57:35Z\" duration=\"0.023031\" asserts=\"0\">" +
                "  <failure>" +
                "    <message><![CDATA[System.ArgumentException : Value does not fall within the expected range.]]></message>" +
                "    <stack-trace><![CDATA[   at NUnitWithDotNetCoreRC2.Test.CalculatorTests.ErrorTest() in D:\\Src\\test\\NUnitWithDotNetCoreRC2.Test\\CalculatorTests.cs:line 50]]></stack-trace>" +
                "  </failure>" +
                "</test-case>";

        const string FAILED_TEST_CASE_XML =
                "<test-case id=\"1017\" name=\"FailedTest\" fullname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests.FailedTest\" methodname=\"FailedTest\" classname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests\" runstate=\"Runnable\" seed=\"1355097167\" result=\"Failed\" start-time=\"2016-06-06 19:57:35Z\" end-time=\"2016-06-06 19:57:35Z\" duration=\"0.017533\" asserts=\"1\">" +
                "  <failure>" +
                "    <message><![CDATA[  Expected: 3" +
                "But was:  2" +
                "]]></message>" +
                "    <stack-trace><![CDATA[at NUnitWithDotNetCoreRC2.Test.CalculatorTests.FailedTest() in D:\\Src\\test\\NUnitWithDotNetCoreRC2.Test\\CalculatorTests.cs:line 44" +
                "]]></stack-trace>" +
                "  </failure>" +
                "</test-case>";

        const string TESTCONTEXT_OUTPUT_TEST_CASE_XML =
                "<test-case id=\"1020\" name=\"TestWithTestContextOutput\" fullname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests.TestWithTestContextOutput\" methodname=\"TestWithTestContextOutput\" classname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests\" runstate=\"Runnable\" seed=\"1328488278\" result=\"Passed\" start-time=\"2016-06-06 19:57:35Z\" end-time=\"2016-06-06 19:57:35Z\" duration=\"0.000001\" asserts=\"0\">" +
                "  <output><![CDATA[Test context output" +
                "]]></output>" +
                "</test-case>";

        Mocks.MockTestExecutionListener _listener;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _listener = new Mocks.MockTestExecutionListener();
        }

        [Test]
        public void CanParseTestErrors()
        {
            var testCase = XElement.Parse(ERROR_TEST_CASE_XML);
            var test = _listener.TestParseTestResult(testCase);
            Assert.That(test, Is.Not.Null);
            Assert.That(test.ErrorMessage?.Trim(), Does.StartWith("System.ArgumentException"));
            Assert.That(test.ErrorStackTrace?.Trim(), Does.StartWith("at NUnitWithDotNetCoreRC2.Test.CalculatorTests.ErrorTest()"));
            Assert.That(test.Outcome, Is.EqualTo(TestOutcome.Failed));
            Assert.That(test.StartTime.Hour, Is.EqualTo(19));
            Assert.That(test.EndTime.Minute, Is.EqualTo(57));
            Assert.That(test.Duration.TotalSeconds, Is.EqualTo(0.023031).Within(0.001d));
        }

        [Test]
        public void CanParseTestFailures()
        {
            var testCase = XElement.Parse(FAILED_TEST_CASE_XML);
            var test = _listener.TestParseTestResult(testCase);
            Assert.That(test, Is.Not.Null);
            Assert.That(test.ErrorMessage?.Trim(), Does.StartWith("Expected: 3"));
            Assert.That(test.ErrorStackTrace?.Trim(), Does.StartWith("at NUnitWithDotNetCoreRC2.Test.CalculatorTests.FailedTest()"));
            Assert.That(test.Outcome, Is.EqualTo(TestOutcome.Failed));
            Assert.That(test.StartTime.Hour, Is.EqualTo(19));
            Assert.That(test.EndTime.Minute, Is.EqualTo(57));
            Assert.That(test.Duration.TotalSeconds, Is.EqualTo(0.017533).Within(0.001d));
        }

        [Test]
        public void CanParseTestOutput()
        {
            var testCase = XElement.Parse(TESTCONTEXT_OUTPUT_TEST_CASE_XML);
            var test = _listener.TestParseTestResult(testCase);
            Assert.That(test, Is.Not.Null);
            Assert.That(test.Messages.Count, Is.EqualTo(1));
            Assert.That(test.Messages[0], Does.StartWith("Test context output"));
        }
    }
}