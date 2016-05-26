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

using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using NUnit.Engine;

namespace NUnit.Runner
{
    public class ResultReporter
    {
        ColorConsoleWriter _writer;
        CommandLineOptions _options;
        XElement _result;
        int _reportIndex = 0;
        string _overallResult;

        public ResultReporter(ResultSummary summary, ColorConsoleWriter writer, CommandLineOptions options)
        {
            _writer = writer;
            _options = options;

            Summary = summary;

            TestResults = Summary.GetTestResults();

            _result = TestResults.FirstNode as XElement;

            _overallResult = Summary.Result;
            if (_overallResult == "Skipped")
                _overallResult = "Warning";
        }

        public ResultSummary Summary { get; }

        public XDocument TestResults { get; }

        /// <summary>
        /// Reports the results to the console
        /// </summary>
        public void ReportResults()
        {
            _writer.WriteLine();

            if (Summary.ExplicitCount + Summary.SkipCount + Summary.IgnoreCount > 0)
                WriteNotRunReport();

            if (_overallResult == "Failed")
                WriteErrorsAndFailuresReport();

            WriteRunSettingsReport();

            WriteSummaryReport();
        }

        #region Summary Report

        public void WriteRunSettingsReport()
        {
            var firstSuite = _result.Element("test-suite");
            if (firstSuite != null)
            {
                var settings = firstSuite.Element("settings")?.Elements("setting");

                if (settings != null && settings.Count() > 0)
                {
                    _writer.WriteLine(ColorStyle.SectionHeader, "Run Settings");

                    foreach (XElement node in settings)
                    {
                        string name = node.Attribute("name")?.Value;
                        string val = node.Attribute("value")?.Value;
                        _writer.WriteLabelLine($"    {name}: ", val);
                    }
                    _writer.WriteLine();
                }
            }
        }

        public void WriteSummaryReport()
        {
            ColorStyle overall = _overallResult == "Passed"
                ? ColorStyle.Pass
                : _overallResult == "Failed"
                    ? ColorStyle.Failure
                    : _overallResult == "Warning"
                        ? ColorStyle.Warning
                        : ColorStyle.Output;

            _writer.WriteLine(ColorStyle.SectionHeader, "Test Run Summary");
            _writer.WriteLabelLine("  Overall result: ", _overallResult, overall);

            WriteSummaryCount("  Test Count: ", Summary.TestCount);
            WriteSummaryCount(", Passed: ", Summary.PassCount);
            WriteSummaryCount(", Failed: ", Summary.FailedCount, ColorStyle.Failure);
            WriteSummaryCount(", Inconclusive: ", Summary.InconclusiveCount);
            WriteSummaryCount(", Skipped: ", Summary.TotalSkipCount);
            _writer.WriteLine();

            if (Summary.FailedCount > 0)
            {
                WriteSummaryCount("    Failed Tests - Failures: ", Summary.FailureCount);
                WriteSummaryCount(", Errors: ", Summary.ErrorCount, ColorStyle.Error);
                WriteSummaryCount(", Invalid: ", Summary.InvalidCount);
                _writer.WriteLine();
            }
            if (Summary.TotalSkipCount > 0)
            {
                WriteSummaryCount("    Skipped Tests - Ignored: ", Summary.IgnoreCount);
                WriteSummaryCount(", Explicit: ", Summary.ExplicitCount);
                WriteSummaryCount(", Other: ", Summary.SkipCount);
                _writer.WriteLine();
            }

            _writer.WriteLabelLine("  Start time: ", Summary.StartTime.ToString("u"));
            _writer.WriteLabelLine("    End time: ", Summary.EndTime.ToString("u"));
            _writer.WriteLabelLine("    Duration: ", $"{Summary.Duration.ToString("0.000")} seconds");
            _writer.WriteLine();
        }

        #endregion

        #region Errors and Failures Report

        public void WriteErrorsAndFailuresReport()
        {
            _reportIndex = 0;
            _writer.WriteLine(ColorStyle.SectionHeader, "Errors and Failures");
            _writer.WriteLine();
            WriteErrorsAndFailures(_result);

            if (_options.StopOnError)
            {
                _writer.WriteLine(ColorStyle.Failure, "Execution terminated after first error");
                _writer.WriteLine();
            }
        }

        void WriteErrorsAndFailures(XElement result)
        {
            string resultState = result.Attribute("result")?.Value;

            switch (result.Name?.ToString())
            {
                case "test-case":
                    if (resultState == "Failed")
                        WriteSingleResult(result, ColorStyle.Failure);
                    return;

                case "test-run":
                    foreach (XElement childResult in result.Elements())
                        WriteErrorsAndFailures(childResult);
                    break;

                case "test-suite":
                    if (resultState == "Failed")
                    {
                        if (result.Attribute("type")?.Value == "Theory")
                        {
                            WriteSingleResult(result, ColorStyle.Failure);
                        }
                        else
                        {
                            var site = result.Attribute("site")?.Value;
                            if (site != "Parent" && site != "Child")
                                WriteSingleResult(result, ColorStyle.Failure);
                            if (site == "SetUp") return;
                        }
                    }

                    foreach (XElement childResult in result.Elements())
                        WriteErrorsAndFailures(childResult);

                    break;
            }
        }

        #endregion

        #region Not Run Report

        public void WriteNotRunReport()
        {
            _reportIndex = 0;
            _writer.WriteLine(ColorStyle.SectionHeader, "Tests Not Run");
            _writer.WriteLine();
            WriteNotRunResults(_result);
        }

        void WriteNotRunResults(XElement result)
        {
            switch (result.Name?.ToString())
            {
                case "test-case":
                    string status = result.Attribute("result")?.Value;

                    if (status == "Skipped")
                    {
                        string label = result.Attribute("label")?.Value;

                        var colorStyle = label == "Ignored"
                            ? ColorStyle.Warning
                            : ColorStyle.Output;

                        WriteSingleResult(result, colorStyle);
                    }
                    break;

                case "test-suite":
                case "test-run":
                    foreach (XElement childResult in result.Elements())
                        WriteNotRunResults(childResult);

                    break;
            }
        }

        #endregion

        #region Helper Methods

        void WriteSummaryCount(string label, int count)
        {
            _writer.WriteLabel(label, count.ToString(CultureInfo.CurrentUICulture));
        }

        void WriteSummaryCount(string label, int count, ColorStyle color)
        {
            _writer.WriteLabel(label, count.ToString(CultureInfo.CurrentUICulture), count > 0 ? color : ColorStyle.Value);
        }

        static readonly char[] EOL_CHARS = { '\r', '\n' };

        void WriteSingleResult(XElement result, ColorStyle colorStyle)
        {
            string status = result.Attribute("label")?.Value;
            if (status == null)
                status = result.Attribute("result")?.Value;

            if (status == "Failed" || status == "Error")
            {
                var site = result.Attribute("site")?.Value;
                if (site == "SetUp" || site == "TearDown")
                    status = site + " " + status;
            }

            string fullName = result.Attribute("fullname")?.Value;

            _writer.WriteLine(colorStyle, $"{++_reportIndex}) {status} : {fullName}");

            XElement failureNode = result.Element("failure");
            if (failureNode != null)
            {
                string message = failureNode.Element("message")?.Value;
                string stacktrace = failureNode.Element("stack-trace")?.Value;

                // In order to control the format, we trim any line-end chars
                // from end of the strings we write and supply them via calls
                // to WriteLine(). Newlines within the strings are retained.

                if (message != null)
                    _writer.WriteLine(colorStyle, message.TrimEnd(EOL_CHARS));

                if (stacktrace != null)
                    _writer.WriteLine(colorStyle, stacktrace.TrimEnd(EOL_CHARS));
            }

            XElement reasonNode = result.Element("reason");
            if (reasonNode != null)
            {
                string message = reasonNode.Element("message")?.Value;

                if (message != null)
                    _writer.WriteLine(colorStyle, message.TrimEnd(EOL_CHARS));
            }

            _writer.WriteLine(); // Skip after each item
        }

        #endregion
    }
}
