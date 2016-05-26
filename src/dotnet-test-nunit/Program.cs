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
using System.Net.Sockets;
using System.Reflection;
using Microsoft.Extensions.Testing.Abstractions;
using NUnit.Options;
using NUnit.Engine.Compatibility;
using System.Diagnostics;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using NUnit.Runner.Sinks;
using System.Collections.Generic;
using NUnit.Engine;
using System.Xml.Linq;
using NUnit.Runner.Interfaces;
using NUnit.Runner.TestListeners;

namespace NUnit.Runner
{
    public class Program : IDisposable
    {
        CommandLineOptions _options;
        Lazy<ColorConsoleWriter> _console;
        ColorConsoleWriter ColorConsole => _console.Value;

        ITestDiscoverySink _testDiscoverySink;
        ITestExecutionSink _testExecutionSink;
        Socket _socket;

        public static int Main(string[] args)
        {
            using (var program = new Program())
                return program.Run(args);
        }

        public Program()
        {
            _console = new Lazy<ColorConsoleWriter>(() => new ColorConsoleWriter(!_options.NoColor));
        }

        public int Run(string[] args)
        {
            try
            {
                _options = new CommandLineOptions(args);
            }
            catch (OptionException ex)
            {
                WriteHeader();
                ColorConsole.WriteLine(ColorStyle.Error, string.Format(ex.Message, ex.OptionName));
                return ReturnCodes.INVALID_ARG;
            }

#if NET451
            if (_options.Debug)
                Debugger.Launch();
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
                IList<string> testList;

                // TODO: Add in test settings
                var settings = new Dictionary<string, object>();

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

                // TODO: Apply filters and merge with testList
                var filter = "<filter />";

                // Load the test framework
                foreach(var assembly in _options.InputFiles)
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
                    if (_options.List)
                    {
                        ITestListener listener = new TestExploreListener(_testDiscoverySink, _options, assemblyPath);
                        //driver.Explore(listener.OnTestEvent, filter);
                    }
                    else
                    {
                        ITestListener listener = new TestExecutionListener(_testExecutionSink, _options, assemblyPath);
                        string xml = driver.Run(listener.OnTestEvent, filter);
                        // TODO: Summarize and save test results
                    }
                }

                if(_options.DesignTime)
                {
                    if (_options.List)
                        _testDiscoverySink.SendTestCompleted();
                    else
                        _testExecutionSink.SendTestCompleted();
                }
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

            return ReturnCodes.OK;
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }

        public Assembly LoadAssembly(string filename)
        {
#if NET451
            return Assembly.LoadFrom(filename);
#else
            var assemblyName = System.IO.Path.GetFileNameWithoutExtension(filename);
            return Assembly.Load(new AssemblyName(assemblyName));
#endif
        }

        void SetupRemoteTestSinks(Stream stream)
        {
            var binaryWriter = new BinaryWriter(stream);
            _testDiscoverySink = new RemoteTestDiscoverySink(binaryWriter);
            _testExecutionSink = new RemoteTestExecutionSink(binaryWriter);
        }
        void SetupConsoleTestSinks()
        {
            _testDiscoverySink = new StreamingTestDiscoverySink(Console.OpenStandardOutput());
            _testExecutionSink = new StreamingTestExecutionSink(Console.OpenStandardOutput());
        }

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
                    configText = string.Format("({0})", ((AssemblyConfigurationAttribute)attrs[0]).Configuration);
                }
            }

            ColorConsole.WriteLine(ColorStyle.Header, string.Format("{0} {1} {2}", programName, versionText, configText));
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
    }
}
