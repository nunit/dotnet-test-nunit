using System;

namespace test_project
{
    using NUnit.Framework;

    [TestFixture]
    public class UnitTests
    {
        [Test]
        public void TestFailed()
        {
            Assert.True(false, "error details");
        }

        [Test]
        public void TestIgnored()
        {
            Assert.Ignore("skip reason");
        }

        [Test]
        public void TestPassed()
        {
            Console.WriteLine("some text");
        }
    }
}
