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
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Testing.Abstractions;
using NUnit.Runner.Extensions;

namespace NUnit.Runner.TestListeners
{
    public class TestEventArgs : EventArgs
    {
        public string TestName { get; }

        public string TestOutput { get; }

        public TestEventArgs(string testName, string testOutput)
        {
            TestName = testName;
            TestOutput = testOutput;
        }
    }
    public class TestOutputEventArgs : TestEventArgs
    {
        public string Stream { get; }

        public TestOutputEventArgs(string testName, string testOutput, string stream)
            : base(testName, testOutput)
        {
            Stream = stream;
        }
    }

    public class TestExecutionListener : BaseTestListener
    {
        public event EventHandler<TestEventArgs> TestStarted;
        public event EventHandler<TestEventArgs> TestFinished;
        public event EventHandler<TestEventArgs> SuiteFinished;
        public event EventHandler<TestOutputEventArgs> TestOutput;

        readonly ITestExecutionSink _sink;

        public TestExecutionListener(ITestExecutionSink sink, CommandLineOptions options, string assemblyPath)
            : base(options, assemblyPath)
        {
            _sink = sink;
        }

        public override void OnTestEvent(string xml)
        {
            var element = XElement.Parse(xml);
            switch (element?.Name?.LocalName)
            {
                case "start-suite":
                    break;
                case "test-suite":
                    OnTestSuite(element);
                    break;
                case "start-test":
                    OnStartTest(element);
                    break;
                case "test-case":
                    OnTestCase(element);
                    break;
                case "test-output":
                    OnTestOutput(element);
                    break;
                default:
                    //Console.WriteLine(xml);
                    break;
            }
        }

        void OnStartTest(XElement xml)
        {
            var test = ParseTest(xml);

            TestStarted?.Invoke(this, new TestEventArgs(test.FullyQualifiedName, null));

            if (Options.DesignTime)
                _sink?.SendTestStarted(test);
        }

        void OnTestCase(XElement xml)
        {
            var testResult = ParseTestResult(xml);

            string output = null;

            if (testResult.Messages.Count > 0)
                output = testResult.Messages[0];
            else if (testResult.Outcome != TestOutcome.None)
                output = testResult.Outcome.ToString();            

            TestFinished?.Invoke(this, new TestEventArgs(testResult.Test.FullyQualifiedName, output));

            if (Options.DesignTime)
                _sink?.SendTestResult(testResult);
        }

        void OnTestSuite(XElement xml)
        {
            var testName = xml.Attribute("fullname")?.Value;
            var output = xml.Elements("output").FirstOrDefault()?.Value;

            SuiteFinished?.Invoke(this, new TestEventArgs(testName, output));
        }

        void OnTestOutput(XElement xml)
        {
            var testName = xml.Attribute("testname")?.Value;
            var stream = xml.Attribute("stream")?.Value;
            var output = xml.Value;
            TestOutput?.Invoke(this, new TestOutputEventArgs(testName, output, stream));
        }

        protected TestResult ParseTestResult(XElement xml)
        {
            var test = ParseTest(xml);
            var outcomeAttr = xml.Attribute("result").ConvertToTestOutcome();

            var testResult = new TestResult(test)
            {
                StartTime = xml.Attribute("start-time").ConvertToDateTime(),
                EndTime = xml.Attribute("end-time").ConvertToDateTime(),
                Outcome = outcomeAttr,
                Duration = xml.Attribute("duration").ConvertToTimeSpan(outcomeAttr),
                ComputerName = Environment.MachineName
            };
            // Output, Messages and stack traces
            testResult.ErrorMessage = GetErrorMessage(xml);
            testResult.ErrorStackTrace = xml.Element("failure")?.Element("stack-trace")?.Value;
            string messages = xml.Element("output")?.Value;
            if (!string.IsNullOrWhiteSpace(messages))
                testResult.Messages.Add(messages);
            return testResult;
        }

        string GetErrorMessage(XElement xml)
        {
            var message = xml.Element("failure")?.Element("message")?.Value;
            if(!string.IsNullOrWhiteSpace(message))
            {
                // If we're running in the IDE, remove any caret line from the message
                // since it will be displayed using a variable font and won't make sense.
                if (Options.DesignTime)
                {
                    string pattern = Environment.NewLine + "  -*\\^" + Environment.NewLine;
                    message = Regex.Replace(message, pattern, Environment.NewLine, RegexOptions.Multiline);
                }
            }
            else
            {
                message = xml.Element("reason")?.Element("message")?.Value;
            }
            return message;
        }
    }
}
