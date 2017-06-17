// ***********************************************************************
// Copyright (c) 2016 NUnit Project
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
using System.Xml.Linq;
using Microsoft.Extensions.Testing.Abstractions;
using NUnit.Runner.Extensions;
using NUnit.Runner.Interfaces;
using NUnit.Runner.Navigation;

namespace NUnit.Runner.TestListeners
{
    public abstract class BaseTestListener : ITestListener
    {
        readonly string _assemblyPath;
        readonly NavigationDataProvider _provider;

        protected CommandLineOptions Options { get; }

        /// <summary>
        /// Constructs a <see cref="BaseTestListener"/>
        /// </summary>
        /// <param name="options">The command line options passed into this run</param>
        /// <param name="assemblyPath">The full path of the assembly that is being executed or explored</param>
        public BaseTestListener(CommandLineOptions options, string assemblyPath)
        {
            Options = options;
            _assemblyPath = assemblyPath;
            _provider = NavigationDataProvider.GetNavigationDataProvider(assemblyPath);
        }

        public abstract void OnTestEvent(string xml);

        protected Test ParseTest(XElement xml)
        {
            var className = xml.Attribute("classname")?.Value;
            var methodName = xml.Attribute("methodname")?.Value;
            var sourceData = _provider?.GetSourceData(className, methodName);

            //use the _assemblyPath, the test case Id attribute,
            //and the fullName attribute to generate a unique signature
            //for this test.
            //
            //Originally, just the id was used, but the id from the
            //xml attribute is not sufficient, because different
            //projects in the same solution will generate the same
            //id.  In "Design" mode, this causes a conflict within
            //Visual Studio and causes tests to get all whacked up.
            //This is a fix for dotnet-test-nunit#58
            //
            //However, another issue was discovered (dotnet-test-nunit#87)
            //Turns out, the assembly name and test id is also not sufficient.
            //During the discovery process, Visual Studio will not
            //update the internal value for FulluyQualifiedName for
            //a test if the Guid does not change.  Therefore, if you
            //change the values of a TestCase, and they have the same
            //test ID, then visual Studio will ignore the new
            //FullyQualifiedName returned during test discovery.
            //While it ignores the new FullyQualifiedName, it will
            //update the DisplayName, which made debugging this
            //very frustrating.  Nonetheless, to force a new
            //guid, we use the fullname of the testcase in the
            //Guid/signature generation.
            string uniqueName = xml.Attribute("id")
                + "|"
                + _assemblyPath
                + "|"
                + xml.Attribute("fullname")?.Value ?? "";
            Guid testSignature = uniqueName.GetSignatureAsGuid();

            var test = new Test
            {
                Id = testSignature,
                DisplayName = xml.Attribute("name")?.Value ?? "",
                FullyQualifiedName = xml.Attribute("fullname")?.Value ?? "",
                CodeFilePath = sourceData?.Filename,
                LineNumber = sourceData?.LineNumber
            };
            // Add properties
            var properties = xml.Descendants("property");
            foreach(var property in properties)
            {
                var name = property.Attribute("name")?.Value ?? "";
                var value = property.Attribute("value")?.Value ?? "";
                // TODO: When there is a way to support multiple categories, look
                // for the Category key and append them.
                test.Properties[name] = value;
            }
            return test;
        }
    }
}
