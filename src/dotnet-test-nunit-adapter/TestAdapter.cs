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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Runner;
using TestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;

namespace NUnit.Adapter
{
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(ExecutorId)]
    [ExtensionUri(ExecutorId)]
    public class TestAdapter : ITestDiscoverer, ITestExecutor
    {
        public const string ExecutorId = "executor://nunit";
        public static readonly Uri ExecutorUri = new Uri(ExecutorId);

        public void DiscoverTests(
            IEnumerable<string> sources,
            IDiscoveryContext discoveryContext,
            IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (discoveryContext == null) throw new ArgumentNullException(nameof(discoveryContext));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (discoverySink == null) throw new ArgumentNullException(nameof(discoverySink));
            using (var console = new Console(str => logger.SendMessage(TestMessageLevel.Informational, str)))
            {
                foreach (var testCase in GetTestCases(sources, console, null))
                {
                    discoverySink.SendTestCase(testCase);
                }
            }
        }

        public void RunTests(
            IEnumerable<TestCase> tests,
            IRunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            if (tests == null) throw new ArgumentNullException(nameof(tests));
            if (runContext == null) throw new ArgumentNullException(nameof(runContext));
            if (frameworkHandle == null) throw new ArgumentNullException(nameof(frameworkHandle));
            using (var console = new Console(str => frameworkHandle.SendMessage(TestMessageLevel.Informational, str)))
            {
                var groupedTests =
                    from test in tests
                    group test by test.Source;

                var first = true;
                foreach (var groupedTest in groupedTests)
                {
                    var source = groupedTest.Key;
                    var testSource = new TestSource(source);
                    var runner = new TestRunner(console, testSource, new TestListener(frameworkHandle, source));
                    var testList = string.Join(",", groupedTest.Select(i => i.FullyQualifiedName));
                    var args = new List<string>{ groupedTest.Key, "--designtime", $"--test={testList}" };
                    if (first)
                    {
                        first = false;
                        args.Add("--noheader");
                    }

                    if (!string.IsNullOrEmpty(runContext.TestRunDirectory))
                    {
                        args.Add($"--work={runContext.TestRunDirectory}");
                    }

                    runner.Run(args.ToArray());
                }
            }
        }

        public void RunTests(
            IEnumerable<string> sources,
            IRunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (runContext == null) throw new ArgumentNullException(nameof(runContext));
            if (frameworkHandle == null) throw new ArgumentNullException(nameof(frameworkHandle));
            var first = true;
            foreach (var source in sources)
            {
                using (var console = new Console(str => frameworkHandle.SendMessage(TestMessageLevel.Informational, str)))
                {
                    var testSource = new TestSource(source);
                    var runner = new TestRunner(console, testSource, new TestListener(frameworkHandle, source));
                    var args = new List<string>{ source, "--designtime" };
                    if (first)
                    {
                        first = false;
                        args.Add("--noheader");
                    }

                    if (!string.IsNullOrEmpty(runContext.TestRunDirectory))
                    {
                        args.Add($"--work={runContext.TestRunDirectory}");
                    }

                    runner.Run(args.ToArray());
                }
            }
        }

        public void Cancel()
        {
        }

        private static IEnumerable<TestCase> GetTestCases(
            IEnumerable<string> sources,
            IConsole console,
            string workingDirectory)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (console == null) throw new ArgumentNullException(nameof(console));

            foreach (var source in sources)
            {
                var testSource = new TestSource(source);
                var runner = new TestRunner(console, testSource);
                var args = new List<string>{source, "--designtime", "--list", "--noheader"};
                if (!string.IsNullOrEmpty(workingDirectory))
                {
                    args.Add($"--work={workingDirectory}");
                }

                runner.Run(args.ToArray());

                foreach (var testCase in testSource)
                {
                    yield return testCase;
                }
            }
        }
    }
}