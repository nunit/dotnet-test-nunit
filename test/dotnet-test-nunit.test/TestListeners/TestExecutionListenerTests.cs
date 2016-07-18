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
using Microsoft.Extensions.Testing.Abstractions;
using NUnit.Framework;
using NUnit.Runner.TestListeners;

namespace NUnit.Runner.Test.TestListeners
{
    [TestFixture]
    public class TestExecutionListenerTests
    {
        const string SUCCESS_TEST_CASE_XML =
            "<test-case id=\"1006\" name=\"CanSubtract(-1,-1,0)\" fullname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests.CanSubtract(-1,-1,0)\" methodname=\"CanSubtract\" classname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests\" runstate=\"Runnable\" seed=\"1663476057\" result=\"Passed\" start-time=\"2016-06-06 23:31:34Z\" end-time=\"2016-06-06 23:31:34Z\" duration=\"0.000001\" asserts=\"1\" />";

        const string IGNORE_TEST_CASE_XML =
                "<test-case id=\"1021\" name=\"IgnoredTest\" fullname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests.IgnoredTest\" methodname=\"IgnoredTest\" classname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests\" runstate=\"Ignored\" seed=\"1580158007\" result=\"Skipped\" label=\"Ignored\" start-time=\"2016-06-06 23:45:17Z\" end-time=\"2016-06-06 23:45:17Z\" duration=\"0.000001\" asserts=\"0\">" +
                "  <properties>" +
                "    <property name=\"_SKIPREASON\" value=\"Ignored Test\" />" +
                "  </properties>" +
                "  <reason>" +
                "    <message><![CDATA[Ignored Test]]></message>" +
                "  </reason>" +
                "</test-case>";

        const string EXPLICIT_TEST_CASE_XML =
                "<test-case id=\"1022\" name=\"ExplicitTest\" fullname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests.ExplicitTest\" methodname=\"ExplicitTest\" classname=\"NUnitWithDotNetCoreRC2.Test.CalculatorTests\" runstate=\"Explicit\" seed=\"1196887609\" result=\"Skipped\" label=\"Explicit\" start-time=\"2016-06-06 23:45:17Z\" end-time=\"2016-06-06 23:45:17Z\" duration=\"0.000505\" asserts=\"0\">" +
                "  <properties>" +
                "    <property name=\"_SKIPREASON\" value=\"Explicit Test\" />" +
                "  </properties>" +
                "  <reason>" +
                "    <message><![CDATA[Explicit Test]]></message>" +
                "  </reason>" +
                "</test-case>";

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


        const string TEST_SUITE_XML =
                "<test-suite type=\"ParameterizedMethod\" id=\"1004\" name=\"LoadWithFrenchCanadianCulture\" fullname=\"NUnit.Framework.Internal.CultureSettingAndDetectionTests.LoadWithFrenchCanadianCulture\" runstate=\"Runnable\" testcasecount=\"1\">" +
                "  <test-case id=\"0-3896\" name=\"LoadWithFrenchCanadianCulture\" fullname=\"NUnit.Framework.Internal.CultureSettingAndDetectionTests.LoadWithFrenchCanadianCulture\" methodname=\"LoadWithFrenchCanadianCulture\" classname=\"NUnit.Framework.Internal.CultureSettingAndDetectionTests\" runstate=\"Runnable\" seed=\"1611686282\" >" +
                "    <properties>" +
                "      <property name = \"SetCulture\" value=\"fr-CA\" />" +
                "      <property name = \"UICulture\" value=\"en-CA\" />" +
                "    </properties>" +
                "  </test-case>" +
                "</test-suite>";

        const string TEST_OUTPUT =
                "<test-output stream=\"theStream\" testname=\"NUnit.Output.Test\">This is the test output</test-output>";

        const string TEST_OUTPUT_WITH_CDATA =
                "<test-output stream=\"theStream\" testname=\"NUnit.Output.CDataTest\"><![CDATA[Test CData output]]></test-output>";

        Mocks.MockTestExecutionSink _sink;
        TestExecutionListener _listener;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _sink = new Mocks.MockTestExecutionSink();
            _listener = new TestExecutionListener(_sink, new CommandLineOptions("--designtime"), @"\src");
        }

        [Test]
        public void CanParseSuccess()
        {
            var eventHandler =
                new EventHandler<TestEventArgs>((sender, args) => Assert.That(args.TestOutput, Is.EqualTo("Passed")));
            _listener.TestFinished += eventHandler;

            _listener.OnTestEvent(SUCCESS_TEST_CASE_XML);
            
            var testResult = _sink.TestResult;
            Assert.That(testResult, Is.Not.Null);
            Assert.That(testResult.ErrorMessage, Is.Null.Or.Empty);
            Assert.That(testResult.ErrorStackTrace, Is.Null.Or.Empty);
            Assert.That(testResult.Outcome, Is.EqualTo(TestOutcome.Passed));
            Assert.That(testResult.StartTime.Hour, Is.EqualTo(23));
            Assert.That(testResult.EndTime.Minute, Is.EqualTo(31));
            Assert.That(testResult.Duration.TotalSeconds, Is.EqualTo(0.000001d).Within(0.001d));

            _listener.TestFinished -= eventHandler;
        }

        [Test]
        public void CanParseIgnoredTests()
        {
            var eventHandler =
                new EventHandler<TestEventArgs>((sender, args) => Assert.That(args.TestOutput, Is.EqualTo("Skipped")));
            _listener.TestFinished += eventHandler;

            _listener.OnTestEvent(IGNORE_TEST_CASE_XML);
            var testResult = _sink.TestResult;
            Assert.That(testResult, Is.Not.Null);
            Assert.That(testResult.ErrorMessage, Is.EqualTo("Ignored Test"));
            Assert.That(testResult.ErrorStackTrace, Is.Null.Or.Empty);
            Assert.That(testResult.Outcome, Is.EqualTo(TestOutcome.Skipped));
            Assert.That(testResult.StartTime.Hour, Is.EqualTo(23));
            Assert.That(testResult.EndTime.Minute, Is.EqualTo(45));
            Assert.That(testResult.Duration.TotalSeconds, Is.EqualTo(0.000001d).Within(0.001d));

            _listener.TestFinished -= eventHandler;
        }

        [Test]
        public void CanParseExplicitTests()
        {
            var eventHandler =
                new EventHandler<TestEventArgs>((sender, args) => Assert.That(args.TestOutput, Is.EqualTo("Skipped")));
            _listener.TestFinished += eventHandler;

            _listener.OnTestEvent(EXPLICIT_TEST_CASE_XML);
            var testResult = _sink.TestResult;
            Assert.That(testResult, Is.Not.Null);
            Assert.That(testResult.ErrorMessage, Is.EqualTo("Explicit Test"));
            Assert.That(testResult.ErrorStackTrace, Is.Null.Or.Empty);
            Assert.That(testResult.Outcome, Is.EqualTo(TestOutcome.Skipped));
            Assert.That(testResult.StartTime.Hour, Is.EqualTo(23));
            Assert.That(testResult.EndTime.Minute, Is.EqualTo(45));
            Assert.That(testResult.Duration.TotalSeconds, Is.EqualTo(0.000001d).Within(0.001d));

            _listener.TestFinished -= eventHandler;
        }

        [Test]
        public void CanParseTestErrors()
        {
            var eventHandler =
                new EventHandler<TestEventArgs>((sender, args) => Assert.That(args.TestOutput, Is.EqualTo("Failed")));
            _listener.TestFinished += eventHandler;

            _listener.OnTestEvent(ERROR_TEST_CASE_XML);
            var testResult = _sink.TestResult;
            Assert.That(testResult, Is.Not.Null);
            Assert.That(testResult.ErrorMessage?.Trim(), Does.StartWith("System.ArgumentException"));
            Assert.That(testResult.ErrorStackTrace?.Trim(), Does.StartWith("at NUnitWithDotNetCoreRC2.Test.CalculatorTests.ErrorTest()"));
            Assert.That(testResult.Outcome, Is.EqualTo(TestOutcome.Failed));
            Assert.That(testResult.StartTime.Hour, Is.EqualTo(19));
            Assert.That(testResult.EndTime.Minute, Is.EqualTo(57));
            Assert.That(testResult.Duration.TotalSeconds, Is.EqualTo(0.023031).Within(0.001d));

            _listener.TestFinished -= eventHandler;
        }

        [Test]
        public void CanParseTestFailures()
        {
            var eventHandler =
                new EventHandler<TestEventArgs>((sender, args) => Assert.That(args.TestOutput, Is.EqualTo("Failed")));
            _listener.TestFinished += eventHandler;

            _listener.OnTestEvent(FAILED_TEST_CASE_XML);
            var testResult = _sink.TestResult;
            Assert.That(testResult, Is.Not.Null);
            Assert.That(testResult.ErrorMessage?.Trim(), Does.StartWith("Expected: 3"));
            Assert.That(testResult.ErrorStackTrace?.Trim(), Does.StartWith("at NUnitWithDotNetCoreRC2.Test.CalculatorTests.FailedTest()"));
            Assert.That(testResult.Outcome, Is.EqualTo(TestOutcome.Failed));
            Assert.That(testResult.StartTime.Hour, Is.EqualTo(19));
            Assert.That(testResult.EndTime.Minute, Is.EqualTo(57));
            Assert.That(testResult.Duration.TotalSeconds, Is.EqualTo(0.017533).Within(0.001d));

            _listener.TestFinished -= eventHandler;
        }

        [Test]
        public void CanParseTestOutput()
        {
            _listener.OnTestEvent(TESTCONTEXT_OUTPUT_TEST_CASE_XML);
            var testResult = _sink.TestResult;
            Assert.That(testResult, Is.Not.Null);
            Assert.That(testResult.Messages.Count, Is.EqualTo(1));
            Assert.That(testResult.Messages[0], Does.StartWith("Test context output"));
        }

        [Test]
        public void FiresTestFinished()
        {
            string test = null;
            string output = null;
            _listener.TestFinished += (sender, args) =>
            {
                test = args.TestName;
                output = args.TestOutput;
            };

            _listener.OnTestEvent(TESTCONTEXT_OUTPUT_TEST_CASE_XML);

            Assert.That(test, Is.Not.Null);
            Assert.That(test, Is.EqualTo("NUnitWithDotNetCoreRC2.Test.CalculatorTests.TestWithTestContextOutput"));

            Assert.That(output, Is.Not.Null);
            Assert.That(output, Does.StartWith("Test context output"));
        }

        [Test]
        public void FiresSuiteFinished()
        {
            string test = null;
            _listener.SuiteFinished += (sender, args) => test = args.TestName;

            _listener.OnTestEvent(TEST_SUITE_XML);

            Assert.That(test, Is.Not.Null);
            Assert.That(test, Is.EqualTo("NUnit.Framework.Internal.CultureSettingAndDetectionTests.LoadWithFrenchCanadianCulture"));
        }

        [TestCase(TEST_OUTPUT, "NUnit.Output.Test", "This is the test output")]
        [TestCase(TEST_OUTPUT_WITH_CDATA, "NUnit.Output.CDataTest", "Test CData output")]
        public void FiresTestOutput(string xml, string expectedTest, string expectedOutput)
        {
            string test = null;
            string stream = null;
            string output = null;
            _listener.TestOutput += (sender, args) =>
            {
                test = args.TestName;
                stream = args.Stream;
                output = args.TestOutput;
            };

            _listener.OnTestEvent(xml);

            Assert.That(test, Is.Not.Null);
            Assert.That(test, Is.EqualTo(expectedTest));

            Assert.That(output, Is.Not.Null);
            Assert.That(output, Is.EqualTo(expectedOutput));

            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.EqualTo("theStream"));
        }
    }
}
