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
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using TestResult = Microsoft.Extensions.Testing.Abstractions.TestResult;

namespace NUnit.Adapter
{
    internal class TestListener : ITestExecutionSink
    {
        private readonly IFrameworkHandle _frameworkHandle;
        private readonly string _source;

        public TestListener(IFrameworkHandle frameworkHandle, string source)
        {
            if (frameworkHandle == null) throw new ArgumentNullException(nameof(frameworkHandle));
            if (source == null) throw new ArgumentNullException(nameof(source));
            _frameworkHandle = frameworkHandle;
            _source = source;
        }

        public void SendWaitingCommand()
        {
        }

        public void SendTestCompleted()
        {
        }

        public void SendTestStarted(Test test)
        {
            if (test == null) throw new ArgumentNullException(nameof(test));
            _frameworkHandle.RecordStart(test.ToTestCase(_source));
        }

        public void SendTestResult(TestResult testResult)
        {
            if (testResult == null) throw new ArgumentNullException(nameof(testResult));
            var testCase = testResult.Test.ToTestCase(_source);
            var result = testResult.ToResult(_source);
            _frameworkHandle.RecordResult(result);
            _frameworkHandle.RecordEnd(testCase, result.Outcome);
        }
    }
}
