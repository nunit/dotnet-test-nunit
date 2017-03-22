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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TestOutcome = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome;
using TestResult = Microsoft.Extensions.Testing.Abstractions.TestResult;

namespace NUnit.Adapter
{
    public static class Converters
    {
        public static TestCase ToTestCase(this Test test, string source)
        {
            if (test == null) throw new ArgumentNullException(nameof(test));
            if (source == null) throw new ArgumentNullException(nameof(source));
            var testCase = new TestCase(test.FullyQualifiedName, TestAdapter.ExecutorUri, source)
            {
                Id = test.Id ?? Guid.NewGuid(),
                DisplayName = test.DisplayName ?? test.FullyQualifiedName
            };

            if (!string.IsNullOrWhiteSpace(test.CodeFilePath))
            {
                testCase.CodeFilePath = test.CodeFilePath;
            }

            if (test.LineNumber != null)
            {
                testCase.LineNumber = test.LineNumber.Value;
            }

            return testCase;
        }

        public static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult ToResult(this TestResult testResult, string source)
        {
            if (testResult == null) throw new ArgumentNullException(nameof(testResult));
            if (source == null) throw new ArgumentNullException(nameof(source));
            var result = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testResult.Test.ToTestCase(source))
            {
                DisplayName = testResult.DisplayName,
                ComputerName = testResult.ComputerName,
                Duration = testResult.Duration,
                StartTime = testResult.StartTime,
                EndTime = testResult.EndTime,
                ErrorMessage = testResult.ErrorMessage,
                ErrorStackTrace = testResult.ErrorStackTrace,
                Outcome = testResult.Outcome.ToTestOutcome()
            };

            if (testResult.Messages != null)
            {
                foreach (var message in testResult.Messages)
                {
                    result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, message));
                }
            }

            return result;
        }

        private static TestOutcome ToTestOutcome(this Microsoft.Extensions.Testing.Abstractions.TestOutcome testResultOutcome)
        {
            switch (testResultOutcome)
            {
                case Microsoft.Extensions.Testing.Abstractions.TestOutcome.Failed:
                    return TestOutcome.Failed;

                case Microsoft.Extensions.Testing.Abstractions.TestOutcome.None:
                    return TestOutcome.None;

                case Microsoft.Extensions.Testing.Abstractions.TestOutcome.Passed:
                    return TestOutcome.Passed;

                case Microsoft.Extensions.Testing.Abstractions.TestOutcome.Skipped:
                    return TestOutcome.Skipped;

                case Microsoft.Extensions.Testing.Abstractions.TestOutcome.NotFound:
                    return TestOutcome.NotFound;

                default:
                    throw new ArgumentOutOfRangeException(nameof(testResultOutcome), testResultOutcome, null);
            }
        }
    }
}
