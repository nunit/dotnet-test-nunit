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
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Testing.Abstractions;

namespace NUnit.Runner.Extensions
{
    /// <summary>
    /// Converts between the Microsoft.Testing.Abstractions classes and the
    /// equivilent NUnit xml
    /// </summary>
    public static class TestExtensions
    {
        const double MIN_DURATION = 0.001d;

        static SHA1 SHA { get; } = SHA1.Create();

        /// <summary>
        /// Takes an NUnit id attribute and converts it to a Guid Id
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static Guid ConvertToGuid(this XAttribute attribute)
        {
            if (attribute == null)
                return Guid.Empty;

            var hash = SHA.ComputeHash(Encoding.UTF8.GetBytes(attribute.Value));
            var guid = new byte[16];
            Array.Copy(hash, guid, 16);
            return new Guid(guid);
        }

        public static DateTimeOffset ConvertToDateTime(this XAttribute attribute)
        {
            var result = DateTimeOffset.UtcNow;

            if (attribute != null)
                DateTimeOffset.TryParse(attribute.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result);

            return result;
        }

        /// <summary>
        /// Converts an NUnit XML duration in seconds into a TimeSpan
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static TimeSpan ConvertToTimeSpan(this XAttribute attribute)
        {
            double duration = MIN_DURATION; // Some runners cannot handle a duration of 0

            if(attribute != null)
                double.TryParse(attribute.Value, out duration);

            return TimeSpan.FromSeconds(Math.Max(duration, MIN_DURATION));
        }

        public static TestOutcome ConvertToTestOutcome(this XAttribute attribute)
        {
            if (attribute == null)
                return TestOutcome.None;

            if(attribute.Value.StartsWith("Passed", StringComparison.Ordinal))
                return TestOutcome.Passed;
            if (attribute.Value.StartsWith("Failed", StringComparison.Ordinal))
                return TestOutcome.Failed;
            if (attribute.Value.StartsWith("Skipped", StringComparison.Ordinal))
                return TestOutcome.Skipped;
            if (attribute.Value.StartsWith("Inconclusive", StringComparison.Ordinal))
                return TestOutcome.Skipped;

            return TestOutcome.None;
        }
    }
}
