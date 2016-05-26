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

using System.Xml.Linq;
using Microsoft.Extensions.Testing.Abstractions;
using NUnit.Runner.Extensions;

namespace NUnit.Runner.TestListeners
{
    public class TestExecutionListener : BaseTestListener
    {
        readonly ITestExecutionSink _sink;

        public TestExecutionListener(ITestExecutionSink sink, CommandLineOptions options, string codepath)
            : base(options, codepath)
        {
            _sink = sink;
        }

        public override void OnTestEvent(string xml)
        {
            var element = XElement.Parse(xml);
            switch (element?.Name?.LocalName)
            {
                case "start-suite":
                case "test-suite":
                    break;
                case "start-test":
                    OnStartTest(element);
                    break;
                case "test-case":
                    OnTestCase(element);
                    break;
                default:
                    //Console.WriteLine(xml);
                    break;
            }
        }

        void OnStartTest(XElement xml)
        {
            var test = ParseTest(xml);

            if(Options.DesignTime)
                _sink?.SendTestStarted(test);
        }

        void OnTestCase(XElement xml)
        {
            var testResult = ParseTestResult(xml);

            if (Options.DesignTime)
                _sink?.SendTestResult(testResult);
        }

        TestResult ParseTestResult(XElement xml)
        {
            var test = ParseTest(xml);
            var testResult = new TestResult(test)
            {
                StartTime = xml.Attribute("start-time").ConvertToDateTime(),
                EndTime = xml.Attribute("end-time").ConvertToDateTime(),
                Duration = xml.Attribute("duration").ConvertToTimeSpan(),
                Outcome = xml.Attribute("result").ConvertToTestOutcome()
                // TODO: ComputerName
            };
            // TODO: Messages and stack traces
            return testResult;
        }
    }
}
