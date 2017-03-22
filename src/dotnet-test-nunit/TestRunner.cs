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

using Microsoft.DotNet.InternalAbstractions;
using Microsoft.Extensions.Testing.Abstractions;
using Newtonsoft.Json;
using NUnit.Engine;
using NUnit.Options;
using NUnit.Runner.Interfaces;
using NUnit.Runner.Sinks;
using NUnit.Runner.TestListeners;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Xml.Linq;
using System;

namespace NUnit.Runner
{
    using Engine.Listeners;
    using Result.Writer.V2;

    public class TestRunner : IDisposable
    {
        CommandLineOptions _options;
        Lazy<IConsole> _console;
        IConsole ColorConsole => _console.Value;

        ITestDiscoverySink _testDiscoverySink;
        ITestExecutionSink _testExecutionSink;
        Socket _socket;
        string _workDirectory;

        public TestRunner(
            IConsole console = null,
            ITestDiscoverySink testDiscoverySink = null,
            ITestExecutionSink testExecutionSink = null)
        {
            _testDiscoverySink = testDiscoverySink;
            _testExecutionSink = testExecutionSink;
            _console = new Lazy<IConsole>(() => console ?? new ColorConsoleWriter(!_options.NoColor));
        }

        public int Run(string[] args)
        {
            try
            {
                _options = new CommandLineOptions(args);
                SetWorkDirectory();
            }
            catch (OptionException ex)
            {
                WriteHeader();
                ColorConsole.WriteLine(ColorStyle.Error, string.Format(ex.Message, ex.OptionName));
                return ReturnCodes.INVALID_ARG;
            }

#if NET451 || NET46
            if (_options.Debug)
                System.Diagnostics.Debugger.Launch();
#endif

            if (_options.ShowVersion || !_options.NoHeader)
                WriteHeader();

            if (_options.ShowHelp || args.Length == 0)
            {
                WriteHelpText();
                return ReturnCodes.OK;
            }

            // We already showed version as a part of the header
            if (_options.ShowVersion)
                return ReturnCodes.OK;

            if (!_options.Validate())
            {
                using (new ColorConsole(ColorStyle.Error))
                {
                    foreach (string message in _options.ErrorMessages)
                        Console.Error.WriteLine(message);
                }
                return ReturnCodes.INVALID_ARG;
            }

            if (_options.InputFiles.Count == 0)
            {
                using (new ColorConsole(ColorStyle.Error))
                    Console.Error.WriteLine("Error: no inputs specified");
                return ReturnCodes.OK;
            }

            try
            {
                return Execute();
            }
            //catch (NUnitEngineException ex)
            //{
            //    ColorConsole.WriteLine(ColorStyle.Error, ex.Message);
            //    return ReturnCodes.INVALID_ARG;
            //}
            //catch (TestSelectionParserException ex)
            //{
            //    ColorConsole.WriteLine(ColorStyle.Error, ex.Message);
            //    return ReturnCodes.INVALID_ARG;
            //}
            catch (FileNotFoundException ex)
            {
                ColorConsole.WriteLine(ColorStyle.Error, ex.Message);
                return ReturnCodes.INVALID_ASSEMBLY;
            }
            catch (DirectoryNotFoundException ex)
            {
                ColorConsole.WriteLine(ColorStyle.Error, ex.Message);
                return ReturnCodes.INVALID_ASSEMBLY;
            }
            catch (Exception ex)
            {
                ColorConsole.WriteLine(ColorStyle.Error, ex.ToString());
                return ReturnCodes.UNEXPECTED_ERROR;
            }
            finally
            {
                if (_options.WaitBeforeExit)
                {
                    using (new ColorConsole(ColorStyle.Warning))
                    {
                        Console.Out.WriteLine("\nPress any key to continue . . .");
                        Console.ReadKey(true);
                    }
                }
            }
        }

        int Execute()
        {
            DisplayRuntimeEnvironment();

            DisplayTestFiles();

            IEnumerable<string> testList = SetupSinks();
            IDictionary<string, object> settings = GetTestSettings();

            // We display the filters at this point so  that any exception message
            // thrown by CreateTestFilter will be understandable.
            DisplayTestFilters();

            // Apply filters and merge with testList
            var filter = CreateTestFilter(testList);

            var summary = new ResultSummary();

            // Load the test framework
            foreach (var assembly in _options.InputFiles)
            {
                // TODO: Load async
                var assemblyPath = System.IO.Path.GetFullPath(assembly);
                var testAssembly = LoadAssembly(assemblyPath);
                var frameworkPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(assemblyPath), "nunit.framework.dll");
                var framework = LoadAssembly(frameworkPath);

                var driver = new NUnitPortableDriver();
                var result = driver.Load(framework, testAssembly, settings);

                // TODO: Run async
                // Explore or Run
                if (_options.List || _options.Explore)
                {
                    ITestListener listener = new TestExploreListener(_testDiscoverySink, _options, assemblyPath);
                    string xml = driver.Explore(filter.Text);
                    listener.OnTestEvent(xml);
                    summary.AddResult(xml);
                }
                else
                {
                    var tcListener = new TeamCityEventListener();
                    TestExecutionListener listener = new TestExecutionListener(_testExecutionSink, _options, assemblyPath);
                    SetupLabelOutput(listener);
                    string xml = driver.Run(
                        report =>
                            {
                                listener.OnTestEvent(report);
                                if (_options.TeamCity)
                                {
                                    tcListener.OnTestEvent(report);
                                }
                            },
                        filter.Text);
                    summary.AddResult(xml);
                }
            }

            if (_options.List || _options.Explore)
            {
                if (_options.DesignTime)
                    _testDiscoverySink.SendTestCompleted();

                return ReturnCodes.OK;
            }

            if (_options.DesignTime)
            {
                _testExecutionSink.SendTestCompleted();
                return ReturnCodes.OK;
            }

            // Summarize and save test results
            var reporter = new ResultReporter(summary, ColorConsole, _options);
            reporter.ReportResults();

            // Save out the TestResult.xml
            SaveTestResults(reporter.TestResults);

            if (summary.UnexpectedError)
                return ReturnCodes.UNEXPECTED_ERROR;

            if (summary.InvalidAssemblies > 0)
                return ReturnCodes.INVALID_ASSEMBLY;

            if (summary.InvalidTestFixtures > 0)
                return ReturnCodes.INVALID_TEST_FIXTURE;

            // Return the number of test failures
            return summary.FailedCount;
        }

        void SetupLabelOutput(TestExecutionListener listener)
        {
            var labels = _options.DisplayTestLabels != null
                 ? _options.DisplayTestLabels.ToUpperInvariant()
                 : "ON";

            listener.TestStarted += (sender, args) =>
            {
                if (labels == "ALL")
                    WriteLabelLine(args.TestName);
            };

            listener.TestFinished += (sender, args) =>
            {                
                if (args.TestOutput != null)
                {
                    if (labels == "ON")
                        WriteLabelLine(args.TestName);

                    WriteOutputLine(args.TestOutput);
                }
            };

            listener.SuiteFinished += (sender, args) =>
            {
                if (args.TestOutput != null)
                {
                    if (labels == "ON" || labels == "ALL")
                        WriteLabelLine(args.TestName);

                    WriteOutputLine(args.TestOutput);
                }
            };

            listener.TestOutput += (sender, args) =>
            {
                if (labels == "ON" && args.TestName != null)
                    WriteLabelLine(args.TestName);

                WriteOutputLine(args.TestOutput, args.Stream == "Error" ? ColorStyle.Error : ColorStyle.Output);
            };
        }

        string _currentLabel;

        void WriteLabelLine(string label)
        {
            if (label != _currentLabel)
            {
                ColorConsole.WriteLine(ColorStyle.SectionHeader, $"=> {label}");
                _currentLabel = label;
            }
        }
        private void WriteOutputLine(string text)
        {
            WriteOutputLine(text, ColorStyle.Output);
        }

        private void WriteOutputLine(string text, ColorStyle color)
        {
            ColorConsole.Write(color, text);

            // Some labels were being shown on the same line as the previous output
            if (!text.EndsWith("\n"))
            {
                ColorConsole.WriteLine();
            }
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }

        #region Helper Methods

        Assembly LoadAssembly(string filename)
        {
#if NET451
            return Assembly.LoadFrom(filename);
#else
            var assemblyName = System.IO.Path.GetFileNameWithoutExtension(filename);
            return Assembly.Load(new AssemblyName(assemblyName));
#endif
        }

        IDictionary<string, object> GetTestSettings()
        {
            IDictionary<string, object> settings = new Dictionary<string, object>();

            if (_options.DefaultTimeout >= 0)
                settings.Add(FrameworkSettings.DefaultTimeout, _options.DefaultTimeout);

            if (_options.InternalTraceLevelSpecified)
                settings.Add(FrameworkSettings.InternalTraceLevel, _options.InternalTraceLevel);

            // Always add work directory, in case current directory is changed
            var workDirectory = _options.WorkDirectory ?? Directory.GetCurrentDirectory();
            settings.Add(FrameworkSettings.WorkDirectory, workDirectory);

            if (_options.StopOnError)
                settings.Add(FrameworkSettings.StopOnError, true);

#if false
            if (options.NumberOfTestWorkersSpecified)
                package.Add(FrameworkSettings.NumberOfTestWorkers, options.NumberOfTestWorkers);
#endif

            if (_options.RandomSeedSpecified)
                settings.Add(FrameworkSettings.RandomSeed, _options.RandomSeed);

#if NET451
            if (_options.Debug)
            {
                settings.Add(FrameworkSettings.DebugTests, true);
#if false
                if (!options.NumberOfTestWorkersSpecified)
                    package.Add(FrameworkSettings.NumberOfTestWorkers, 0);
#endif
            }
#endif

            if (_options.DefaultTestNamePattern != null)
                settings.Add(FrameworkSettings.DefaultTestNamePattern, _options.DefaultTestNamePattern);

            if (_options.TestParameters != null)
                settings.Add(FrameworkSettings.TestParameters, _options.TestParameters);

            return settings;
        }

        TestFilter CreateTestFilter(IEnumerable<string> testList)
        {
            ITestFilterBuilder builder = new TestFilterBuilder();

            foreach (string testName in testList)
                builder.AddTest(testName);

            foreach (string testName in _options.TestList)
                builder.AddTest(testName);

            if (_options.WhereClauseSpecified)
                builder.SelectWhere(_options.WhereClause);

            return builder.GetFilter();
        }

        void SetWorkDirectory()
        {
            _workDirectory = _options.WorkDirectory;

            if (_workDirectory == null)
                _workDirectory = Env.DefaultWorkDirectory;
            else if (!Directory.Exists(_workDirectory))
                Directory.CreateDirectory(_workDirectory);
        }

        void SaveTestResults(XDocument testResults)
        {
            foreach (var spec in _options.ResultOutputSpecifications)
            {
                if (!string.IsNullOrWhiteSpace(spec.Transform))
                {
                    ColorConsole.WriteLine(ColorStyle.Error, $"XML Transforms are not currently supported, skipping");
                    continue;
                }

                var outputPath = Path.Combine(_workDirectory, spec.OutputPath);
                try
                {
                    using (var writer = new FileStream(outputPath, FileMode.Create))
                        testResults.Save(writer);

                    ColorConsole.WriteLine(ColorStyle.Default, $"Results saved as {outputPath}");

                    // Report on unsupported specifications instead of failing
                    if ("NUNIT2".Equals(spec.Format.ToUpperInvariant()))
                    {
                        var v2OutputPath = Path.Combine(
                            _workDirectory,
                            $"{Path.GetFileNameWithoutExtension(spec.OutputPath)}.v2{Path.GetExtension(spec.OutputPath)}");

                        new NUnit2XmlResultWriter().WriteResultFile(outputPath, $"{v2OutputPath}");
                        ColorConsole.WriteLine(ColorStyle.Default, $"Results with v2 format saved as {v2OutputPath}");
                    }
                    else if (!"NUNIT3".Equals(spec.Format.ToUpperInvariant()))
                    {
                        ColorConsole.WriteLine(ColorStyle.Error, $"'{spec.Format}' format is not supported.");
                    }
                }
                catch (Exception ex)
                {
                    ColorConsole.Write(ColorStyle.Error, $"Failed to write result file {spec.OutputPath}");
                    ColorConsole.Write(ColorStyle.Error, $"  Error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Test Sinks

        IEnumerable<string> SetupSinks()
        {
            IEnumerable<string> testList = Enumerable.Empty<string>();

            if (_options.PortSpecified)
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(new IPEndPoint(IPAddress.Loopback, _options.Port));
                var networkStream = new NetworkStream(_socket);

                SetupRemoteTestSinks(networkStream);

                if (_options.WaitCommand)
                {
                    var reader = new BinaryReader(networkStream);
                    _testExecutionSink.SendWaitingCommand();

                    var rawMessage = reader.ReadString();
                    var message = JsonConvert.DeserializeObject<Message>(rawMessage);

                    testList = message.Payload.ToObject<RunTestsMessage>().Tests;
                }
            }
            else
            {
                SetupConsoleTestSinks();
            }
            return testList;
        }

        void SetupRemoteTestSinks(Stream stream)
        {
            var binaryWriter = new BinaryWriter(stream);
            _testDiscoverySink = _testDiscoverySink ?? new RemoteTestDiscoverySink(binaryWriter);
            _testExecutionSink = _testExecutionSink ?? new RemoteTestExecutionSink(binaryWriter);
        }

        void SetupConsoleTestSinks()
        {
            _testDiscoverySink = _testDiscoverySink ?? new StreamingTestDiscoverySink(Console.OpenStandardOutput());
            _testExecutionSink = _testExecutionSink ?? new StreamingTestExecutionSink(Console.OpenStandardOutput());
        }

        #endregion

        #region Console Output

        void WriteHeader()
        {
            Assembly executingAssembly = typeof(Program).GetTypeInfo().Assembly;
            string versionText = executingAssembly.GetName().Version.ToString(3);

            string programName = "NUnit .NET Core Console Runner";
            string copyrightText = "Copyright (C) 2016 Charlie Poole.\r\nAll Rights Reserved.";
            string configText = String.Empty;

            object[] attrs = executingAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attrs.Length > 0)
                programName = ((AssemblyTitleAttribute)attrs[0]).Title;

            attrs = executingAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            if (attrs.Length > 0)
                copyrightText = ((AssemblyCopyrightAttribute)attrs[0]).Copyright;

            attrs = executingAssembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
            if (attrs.Length > 0)
            {
                string configuration = ((AssemblyConfigurationAttribute)attrs[0]).Configuration;
                if (!String.IsNullOrEmpty(configuration))
                {
                    configText = $"({((AssemblyConfigurationAttribute)attrs[0]).Configuration})";
                }
            }

            ColorConsole.WriteLine(ColorStyle.Header, $"{programName} {versionText} {configText}");
            ColorConsole.WriteLine(ColorStyle.SubHeader, copyrightText);
            ColorConsole.WriteLine();
        }

        void WriteHelpText()
        {
            ColorConsole.WriteLine();
            ColorConsole.WriteLine(ColorStyle.Header, "DOTNET-TEST-NUNIT [inputfiles] [options]");
            ColorConsole.WriteLine();
            ColorConsole.WriteLine(ColorStyle.Default, "Runs a set of NUnit tests from the console.");
            ColorConsole.WriteLine();
            ColorConsole.WriteLine(ColorStyle.SectionHeader, "InputFiles:");
            ColorConsole.WriteLine(ColorStyle.Default, "      One or more assemblies of a recognized type.");
            ColorConsole.WriteLine();
            ColorConsole.WriteLine(ColorStyle.SectionHeader, "Options:");
            using (new ColorConsole(ColorStyle.Default))
            {
                _options.WriteOptionDescriptions(Console.Out);
            }
            ColorConsole.WriteLine();
            ColorConsole.WriteLine(ColorStyle.SectionHeader, "Description:");
            using (new ColorConsole(ColorStyle.Default))
            {
                ColorConsole.WriteLine("      By default, this command runs the tests contained in the");
                ColorConsole.WriteLine("      assemblies specified. If the --explore option");
                ColorConsole.WriteLine("      is used, no tests are executed but a description of the tests");
                ColorConsole.WriteLine("      is saved in the specified or default format.");
                ColorConsole.WriteLine();
                ColorConsole.WriteLine("      The --where option is intended to extend or replace the earlier");
                ColorConsole.WriteLine("      --test, --include and --exclude options by use of a selection expression");
                ColorConsole.WriteLine("      describing exactly which tests to use. Examples of usage are:");
                ColorConsole.WriteLine("          --where:cat==Data");
                ColorConsole.WriteLine("          --where \"method =~ /DataTest*/ && cat = Slow\"");
                ColorConsole.WriteLine();
                ColorConsole.WriteLine("      Care should be taken in combining --where with --test.");
                ColorConsole.WriteLine("      The test and where specifications are implicitly joined using &&, so");
                ColorConsole.WriteLine("      that BOTH sets of criteria must be satisfied in order for a test to run.");
                ColorConsole.WriteLine("      See the docs for more information and a full description of the syntax");
                ColorConsole.WriteLine("      information and a full description of the syntax.");
                ColorConsole.WriteLine();
                ColorConsole.WriteLine("      If --explore is used without any specification following, a list of");
                ColorConsole.WriteLine("      test cases is output to the writer.");
                ColorConsole.WriteLine();
                ColorConsole.WriteLine("      If none of the options {--result, --explore, --noxml} is used,");
                ColorConsole.WriteLine("      NUnit saves the results to TestResult.xml in nunit3 format");
                ColorConsole.WriteLine();
                ColorConsole.WriteLine("      Any transforms provided must handle input in the native nunit3 format.");
                ColorConsole.WriteLine();
            }
        }

        void DisplayRuntimeEnvironment()
        {
            ColorConsole.WriteLine(ColorStyle.SectionHeader, "Runtime Environment");
            ColorConsole.WriteLabelLine("    OS Platform: ", RuntimeEnvironment.OperatingSystemPlatform);
            ColorConsole.WriteLabelLine("     OS Version: ", RuntimeEnvironment.OperatingSystemVersion);
            ColorConsole.WriteLabelLine("        Runtime: ", RuntimeEnvironment.GetRuntimeIdentifier());
            ColorConsole.WriteLine();
        }

        void DisplayTestFiles()
        {
            ColorConsole.WriteLine(ColorStyle.SectionHeader, "Test Files");
            foreach (string file in _options.InputFiles)
                ColorConsole.WriteLine(ColorStyle.Default, "    " + file);
            ColorConsole.WriteLine();
        }

        void DisplayTestFilters()
        {
            if (_options.TestList.Count > 0 || _options.WhereClauseSpecified)
            {
                ColorConsole.WriteLine(ColorStyle.SectionHeader, "Test Filters");

                if (_options.TestList.Count > 0)
                    foreach (string testName in _options.TestList)
                        ColorConsole.WriteLabelLine("    Test: ", testName);

                if (_options.WhereClauseSpecified)
                    ColorConsole.WriteLabelLine("    Where: ", _options.WhereClause.Trim());

                ColorConsole.WriteLine();
            }
        }

        #endregion
    }
}
