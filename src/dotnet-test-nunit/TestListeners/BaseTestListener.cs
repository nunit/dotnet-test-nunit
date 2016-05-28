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
using NUnit.Runner.Interfaces;

namespace NUnit.Runner.TestListeners
{
    public abstract class BaseTestListener : ITestListener
    {
        readonly string _codepath;

        protected CommandLineOptions Options { get; }

        public BaseTestListener(CommandLineOptions options, string codepath)
        {
            Options = options;
            _codepath = codepath;
        }

        public abstract void OnTestEvent(string xml);

        protected Test ParseTest(XElement xml)
        {
            var test = new Test
            {
                Id = xml.Attribute("id").ConvertToGuid(),
                DisplayName = xml.Attribute("name").Value,
                FullyQualifiedName = xml.Attribute("fullname").Value,
                CodeFilePath = _codepath
                // TODO: LineNumber
            };
            // TODO: Add properties
            return test;
        }
    }
}
