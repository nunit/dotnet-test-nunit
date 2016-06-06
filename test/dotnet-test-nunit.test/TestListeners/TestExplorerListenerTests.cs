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
using NUnit.Framework;
using NUnit.Runner.TestListeners;

namespace NUnit.Runner.Test.TestListeners
{
    [TestFixture]
    public class TestExplorerListenerTests
    {
        const string TEST_CASE_XML_WITH_PROPERTIES =
                "<test-suite type=\"ParameterizedMethod\" id=\"1004\" name=\"LoadWithFrenchCanadianCulture\" fullname=\"NUnit.Framework.Internal.CultureSettingAndDetectionTests.LoadWithFrenchCanadianCulture\" runstate=\"Runnable\" testcasecount=\"1\">" +
                "  <test-case id=\"0-3896\" name=\"LoadWithFrenchCanadianCulture\" fullname=\"NUnit.Framework.Internal.CultureSettingAndDetectionTests.LoadWithFrenchCanadianCulture\" methodname=\"LoadWithFrenchCanadianCulture\" classname=\"NUnit.Framework.Internal.CultureSettingAndDetectionTests\" runstate=\"Runnable\" seed=\"1611686282\" >" +
                "    <properties>" +
                "      <property name = \"SetCulture\" value=\"fr-CA\" />" +
                "      <property name = \"UICulture\" value=\"en-CA\" />" +
                "    </properties>" +
                "  </test-case>" +
                "</test-suite>";

        Mocks.MockTestExplorerSink _sink;
        TestExploreListener _listener;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _sink = new Mocks.MockTestExplorerSink();
            _listener = new TestExploreListener(_sink, new CommandLineOptions("--designtime"), @"\src");
        }

        [Test]
        public void CanParseTestsWithProperties()
        {
            _listener.OnTestEvent(TEST_CASE_XML_WITH_PROPERTIES);
            var test = _sink.TestFound;
            Assert.That(test, Is.Not.Null);
            Assert.That(test.DisplayName, Is.EqualTo("LoadWithFrenchCanadianCulture"));
            Assert.That(test.FullyQualifiedName, Is.EqualTo("NUnit.Framework.Internal.CultureSettingAndDetectionTests.LoadWithFrenchCanadianCulture"));
            Assert.That(test.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(test.Properties.Count, Is.EqualTo(2));
            Assert.That(test.Properties["SetCulture"], Is.EqualTo("fr-CA"));
            Assert.That(test.Properties["UICulture"], Is.EqualTo("en-CA"));
        }
    }
}
