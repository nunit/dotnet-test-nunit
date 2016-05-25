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
using System.IO;
using NUnit.Common;
using NUnit.Options;

namespace NUnit.Runner
{
    public class CommandLineOptions : OptionSet
    {
        private bool validated;
        private bool noresult;

        #region Constructor

        public CommandLineOptions(params string[] args)
        {
            ConfigureOptions();
            if (args != null)
                Parse(args);
        }

        #endregion

        #region Properties

        // .Net Core runner options http://dotnet.github.io/docs/core-concepts/core-sdk/cli/dotnet-test-protocol.html
        public bool DesignTime { get; private set; }

        public int Port { get; private set; } = -1;
        public bool PortSpecified => Port >= 0;

        public bool WaitCommand { get; private set; }

        public bool List { get; private set; }

        // Action to Perform

        public bool Explore { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool ShowVersion { get; private set; }

        // Select tests

        public IList<string> InputFiles { get; } = new List<string>();

        public IList<string> TestList { get; } = new List<string>();

        public string WhereClause { get; private set; }
        public bool WhereClauseSpecified => WhereClause != null;

        public int DefaultTimeout { get; private set; } = -1;
        public bool DefaultTimeoutSpecified => DefaultTimeout >= 0;

        public int RandomSeed { get; private set; } = -1;
        public bool RandomSeedSpecified => RandomSeed >= 0;

#if false
        public int NumberOfTestWorkers { get; private set; } = -1;
        public bool NumberOfTestWorkersSpecified => NumberOfTestWorkers >= 0;
#endif

        public bool StopOnError { get; private set; }

        public bool WaitBeforeExit { get; private set; }

#if NET451
        public bool Debug { get; private set; }
#endif

        // Output Control

        public bool NoHeader { get; private set; }

        public bool NoColor { get; private set; }

        public bool Verbose { get; private set; }

        public string OutFile { get; private set; }
        public bool OutFileSpecified => OutFile != null;

        public string ErrFile { get; private set; }
        public bool ErrFileSpecified => ErrFile != null;

        public string DisplayTestLabels { get; private set; }

        string workDirectory = null;
        public string WorkDirectory => workDirectory ?? NUnit.Env.DefaultWorkDirectory;

        public bool WorkDirectorySpecified => workDirectory != null;

        public string InternalTraceLevel { get; private set; }
        public bool InternalTraceLevelSpecified => InternalTraceLevel != null;

        /// <summary>Indicates whether a full report should be displayed.</summary>
        public bool Full { get; private set; }

        private List<OutputSpecification> resultOutputSpecifications = new List<OutputSpecification>();
        public IList<OutputSpecification> ResultOutputSpecifications
        {
            get
            {
                if (noresult)
                    return new OutputSpecification[0];

                if (resultOutputSpecifications.Count == 0)
                    resultOutputSpecifications.Add(new OutputSpecification("TestResult.xml"));

                return resultOutputSpecifications;
            }
        }

        public IList<OutputSpecification> ExploreOutputSpecifications { get; private set; } = new List<OutputSpecification>();

        // Error Processing

        public IList<string> ErrorMessages { get; private set; } = new List<string>();

#endregion

#region Public Methods

        public bool Validate()
        {
            if (!validated)
            {
                CheckOptionCombinations();
                validated = true;
            }
            return ErrorMessages.Count == 0;
        }

#endregion

#region Helper Methods

        void CheckOptionCombinations()
        {
            // TODO: Fill in any validations
        }

        /// <summary>
        /// Case is ignored when val is compared to validValues. When a match is found, the
        /// returned value will be in the canonical case from validValues.
        /// </summary>
        protected string RequiredValue(string val, string option, params string[] validValues)
        {
            if (string.IsNullOrEmpty(val))
                ErrorMessages.Add("Missing required value for option '" + option + "'.");

            bool isValid = true;

            if (validValues != null && validValues.Length > 0)
            {
                isValid = false;

                foreach (string valid in validValues)
                    if (string.Compare(valid, val, StringComparison.OrdinalIgnoreCase) == 0)
                        return valid;

            }

            if (!isValid)
                ErrorMessages.Add(string.Format("The value '{0}' is not valid for option '{1}'.", val, option));

            return val;
        }

        protected int RequiredInt(string val, string option)
        {
            // We have to return something even though the value will
            // be ignored if an error is reported. The -1 value seems
            // like a safe bet in case it isn't ignored due to a bug.
            int result = -1;

            if (string.IsNullOrEmpty(val))
                ErrorMessages.Add("Missing required value for option '" + option + "'.");
            else
            {
                // NOTE: Don't replace this with TryParse or you'll break the CF build!
                try
                {
                    result = int.Parse(val);
                }
                catch (Exception)
                {
                    ErrorMessages.Add("An int value was expected for option '{0}' but a value of '{1}' was used");
                }
            }

            return result;
        }

        private string ExpandToFullPath(string path)
        {
            if (path == null) return null;
            return Path.GetFullPath(path);
        }

        void ConfigureOptions()
        {
            // NOTE: The order in which patterns are added
            // determines the display order for the help.

            // Select Tests
            this.Add("test=", "Comma-separated list of {NAMES} of tests to run or explore. This option may be repeated.",
                v => ((List<string>)TestList).AddRange(TestNameParser.Parse(RequiredValue(v, "--test"))));

            this.Add("where=", "Test selection {EXPRESSION} indicating what tests will be run. See description below.",
                v => WhereClause = RequiredValue(v, "--where"));

            this.Add("timeout=", "Set timeout for each test case in {MILLISECONDS}.",
                v => DefaultTimeout = RequiredInt(v, "--timeout"));

            this.Add("seed=", "Set the random {SEED} used to generate test cases.",
                v => RandomSeed = RequiredInt(v, "--seed"));
#if false
            this.Add("workers=", "Specify the {NUMBER} of worker threads to be used in running tests. If not specified, defaults to 2 or the number of processors, whichever is greater.",
                v => numWorkers = RequiredInt(v, "--workers"));
#endif
            this.Add("stoponerror", "Stop run immediately upon any test failure or error.",
                v => StopOnError = v != null);

            this.Add("wait", "Wait for input before closing console window.",
                v => WaitBeforeExit = v != null);

            // Output Control
            this.Add("work=", "{PATH} of the directory to use for output files. If not specified, defaults to the current directory.",
                v => workDirectory = RequiredValue(v, "--work"));

            this.Add("output|out=", "File {PATH} to contain text output from the tests.",
                v => OutFile = RequiredValue(v, "--output"));

            this.Add("err=", "File {PATH} to contain error output from the tests.",
                v => ErrFile = RequiredValue(v, "--err"));

            this.Add("full", "Prints full report of all test results.",
                v => Full = v != null);

            this.Add("result=", "An output {SPEC} for saving the test results.\nThis option may be repeated.",
                v => resultOutputSpecifications.Add(new OutputSpecification(RequiredValue(v, "--resultxml"))));

            this.Add("explore:", "Display or save test info rather than running tests. Optionally provide an output {SPEC} for saving the test info. This option may be repeated.", v =>
            {
                Explore = true;
                if (v != null)
                    ExploreOutputSpecifications.Add(new OutputSpecification(v));
            });

            this.Add("noresult", "Don't save any test results.",
                v => noresult = v != null);

            this.Add("labels=", "Specify whether to write test case names to the output. Values: Off, On, All",
                v => DisplayTestLabels = RequiredValue(v, "--labels", "Off", "On", "All"));

            this.Add("trace=", "Set internal trace {LEVEL}.\nValues: Off, Error, Warning, Info, Verbose (Debug)",
                v => InternalTraceLevel = RequiredValue(v, "--trace", "Off", "Error", "Warning", "Info", "Verbose", "Debug"));

            this.Add("noheader|noh", "Suppress display of program information at start of run.",
                v => NoHeader = v != null);

            this.Add("nocolor|noc", "Displays console output without color.",
                v => NoColor = v != null);

            this.Add("verbose|v", "Display additional information as the test runs.",
                v => Verbose = v != null);

            this.Add("help|h", "Display this message and exit.",
                v => ShowHelp = v != null);

            this.Add("version|V", "Display the header and exit.",
                v => ShowVersion = v != null);

#if NET451
            this.Add("debug", "Attaches the debugger on launch",
                v => Debug = v != null);
#endif

            // .NET Core runner options
            this.Add("designtime", "Used to indicate that the runner is being launched by an IDE",
                v => DesignTime = v != null);

            this.Add("port=", "Used by IDEs to specify a port number to listen for a connection",
                v => Port = RequiredInt(v, "--port"));

            this.Add("wait-command", "Used by IDEs to indicate that the runner should connect to the port and wait for commands, instead of going ahead and executing the tests",
                v => WaitCommand = v != null);

            this.Add("list", "Used by IDEs to request a list of tests that can be run",
                v => List = v != null);

            // Default
            this.Add("<>", v =>
            {
                if (v.StartsWith("-", StringComparison.Ordinal) || v.StartsWith("/", StringComparison.Ordinal) && Path.DirectorySeparatorChar != '/')
                    ErrorMessages.Add("Invalid argument: " + v);
                else
                    InputFiles.Add(v);
            });
        }

#endregion
    }
}
