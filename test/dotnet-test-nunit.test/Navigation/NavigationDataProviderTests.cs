// ****************************************************************
// Copyright (c) 2016 NUnit Software. All rights reserved.
// ****************************************************************

using System.Reflection;
using NUnit.Framework;
using NUnit.Runner.Navigation;

namespace NUnit.Runner.Test.Navigation
{
    [TestFixture]
    public class NavigationDataProviderTests
    {
        const string Prefix = "NUnit.Runner.Test.Navigation.NavigationTestData";

        NavigationDataProvider _provider;

        [SetUp]
        public void SetUp()
        {
            var path = typeof(NavigationDataProviderTests).GetTypeInfo().Assembly.Location;
            //string path = Path.Combine(Directory.GetCurrentDirectory(), "dotnet-test-nunit.test.dll");
            _provider = NavigationDataProvider.GetNavigationDataProvider(path);
            Assert.That(_provider, Is.Not.Null, $"Could not create the NavigationDataProvider for {path}");
        }

        [TestCase("", "EmptyMethod_OneLine", 9)]
        [TestCase("", "EmptyMethod_TwoLines", 12)]
        [TestCase("", "EmptyMethod_ThreeLines", 16)]
        [TestCase("", "EmptyMethod_LotsOfLines", 20)]
        [TestCase("", "SimpleMethod_Void_NoArgs", 26)]
        [TestCase("", "SimpleMethod_Void_OneArg", 32)]
        [TestCase("", "SimpleMethod_Void_TwoArgs", 38)]
        [TestCase("", "SimpleMethod_ReturnsInt_NoArgs", 44)]
        [TestCase("", "SimpleMethod_ReturnsString_OneArg", 50)]
        // Generic method uses simple name
        [TestCase("", "GenericMethod_ReturnsString_OneArg", 55)]
        [TestCase("", "AsyncMethod_Void", 60)]
        [TestCase("", "AsyncMethod_Task", 67)]
        [TestCase("", "AsyncMethod_ReturnsInt", 74)]
        [TestCase("+NestedClass", "SimpleMethod_Void_NoArgs", 83)]
        [TestCase("+ParameterizedFixture", "SimpleMethod_ReturnsString_OneArg", 101)]
        // Generic Fixture requires ` plus type arg count
        [TestCase("+GenericFixture`2", "Matches", 116)]
        [TestCase("+GenericFixture`2+DoublyNested", "WriteBoth", 132)]
        [TestCase("+GenericFixture`2+DoublyNested`1", "WriteAllThree", 151)]
        public void VerifyNavigationData(string suffix, string methodName, int lineNumber)
        {
            // Get the navigation data - ensure names are spelled correctly!
            var className = Prefix + suffix;
            var data = _provider.GetSourceData(className, methodName);
            Assert.NotNull(data, "Unable to retrieve navigation data");
            Assert.That(data.Filename, Does.EndWith("NavigationTestData.cs"));
            Assert.That(data.LineNumber, Is.EqualTo(lineNumber));
        }
    }
}
