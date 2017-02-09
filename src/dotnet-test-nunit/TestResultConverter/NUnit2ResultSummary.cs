// ***********************************************************************
// Copyright (c) 2014 Charlie Poole
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

namespace NUnit.Runner.TestResultConverter
{
    using Engine.Listeners;
    using System;
    using System.Xml;


    /// <summary>
    /// NUnit2ResultSummary summarizes test results as used in the NUnit2 XML format.
    /// </summary>
    public class NUnit2ResultSummary
    {
        private int successCount;

        private int testsRun;

        /// <summary>
        /// Creates a new instance of <see cref="NUnit2ResultSummary"/>
        /// </summary>
        /// <param name="result">The Result XmlNode.</param>
        internal NUnit2ResultSummary(XmlNode result)
        {
            if (result.Name != "test-run")
            {
                throw new InvalidOperationException(
                    "Expected <test-run> as top-level element but was <" + result.Name + ">");
            }

            this.Name = result.GetAttribute("name");
            this.Duration = result.GetAttribute("duration", 0.0);
            this.StartTime = result.GetAttribute("start-time", DateTime.MinValue);
            this.EndTime = result.GetAttribute("end-time", DateTime.MaxValue);

            this.Summarize(result);
        }

        /// <summary>
        /// Gets the duration of the test run in seconds.
        /// </summary>
        public double Duration { get; }

        /// <summary>
        /// Gets the end time of the test run.
        /// </summary>
        public DateTime EndTime { get; }

        /// <summary>
        /// Returns the number of test cases that had an error.
        /// </summary>
        public int Errors { get; private set; }

        public int ErrorsAndFailures => this.Errors + this.Failures;

        /// <summary>
        /// Returns the number of test cases that failed.
        /// </summary>
        public int Failures { get; private set; }

        public int Ignored { get; private set; }

        /// <summary>
        /// Returns the number of test cases that failed.
        /// </summary>
        public int Inconclusive { get; private set; }

        public string Name { get; }

        /// <summary>
        /// Returns the number of test cases that were not runnable
        /// due to errors in the signature of the class or method.
        /// Such tests are also counted as Errors.
        /// </summary>
        public int NotRunnable { get; private set; }

        /// <summary>
        /// Returns the number of tests that passed
        /// </summary>
        public int Passed => this.successCount;

        /// <summary>
        /// Returns the number of test cases for which results
        /// have been summarized. Any tests excluded by use of
        /// Category or Explicit attributes are not counted.
        /// </summary>
        public int ResultCount { get; private set; }

        /// <summary>
        /// Returns the number of test cases that were skipped.
        /// </summary>
        public int Skipped { get; private set; }

        /// <summary>
        /// Gets the start time of the test run.
        /// </summary>
        public DateTime StartTime { get; }

        public bool Success => this.Failures == 0;

        public int TestsNotRun => this.Skipped + this.Ignored + this.NotRunnable;

        /// <summary>
        /// Returns the number of test cases actually run, which
        /// is the same as ResultCount, less any Skipped, Ignored
        /// or NonRunnable tests.
        /// </summary>
        public int TestsRun => this.testsRun;

        private void ProcessTestCase(XmlNode node)
        {
            this.ResultCount++;

            var outcome = node.GetAttribute("result");
            var label = node.GetAttribute("label");
            if (label != null)
            {
                outcome = label;
            }

            switch (outcome)
            {
                case "Passed":
                    {
                        this.successCount++;
                        this.testsRun++;
                        break;
                    }

                case "Failed":
                    {
                        this.Failures++;
                        this.testsRun++;
                        break;
                    }

                case "Error":
                case "Cancelled":
                    {
                        this.Errors++;
                        this.testsRun++;
                        break;
                    }

                case "Inconclusive":
                    {
                        this.Inconclusive++;
                        this.testsRun++;
                        break;
                    }

                case "NotRunnable": // TODO: Check if this can still occur
                case "Invalid":
                    {
                        this.NotRunnable++;
                        break;
                    }

                case "Ignored":
                    {
                        this.Ignored++;
                        break;
                    }

                case "Skipped":
                default:
                    {
                        this.Skipped++;
                        break;
                    }
            }
        }

        private void Summarize(XmlNode node)
        {
            switch (node.Name)
            {
                case "test-case":
                    {
                        this.ProcessTestCase(node);
                        break;
                    }

                // case "test-suite":
                // case "test-fixture":
                // case "method-group":
                default:
                    {
                        foreach (XmlNode childResult in node.ChildNodes)
                        {
                            this.Summarize(childResult);
                        }

                        break;
                    }
            }
        }
    }
}