// ***********************************************************************
// Copyright (c) 2014 Charlie Poole
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

namespace NUnit.Runner.TestResultConverter
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;

    using Microsoft.DotNet.InternalAbstractions;
    using Engine.Listeners;
    using System.Xml.Linq;
    using Utils;

    public class NUnit2XmlResultWriter
    {
        private XmlWriter xmlWriter;

        /// <summary>
        /// Checks if the output is writable. If the output is not
        /// writable, this method should throw an exception.
        /// </summary>
        /// <param name="outputPath"></param>
        public void CheckWritability(string outputPath)
        {
            using (new StreamWriter(File.Create(outputPath), Encoding.UTF8))
            {
            }
        }

        public void WriteResultFile(XDocument result, string outputPath)
        {
            XmlDocument xmlDocument = result.GetXmlDocument();
            this.WriteResultFile(xmlDocument.LastChild, outputPath);
        }

        public void WriteResultFile(XmlNode result, string outputPath)
        {
            using (var writer = new StreamWriter(File.Create(outputPath), Encoding.UTF8))
            {
                this.WriteResultFile(result, writer);
            }
        }

        public void WriteResultFile(XmlNode result, TextWriter writer)
        {
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Encoding = new UTF8Encoding(false);
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.IndentChars = "\t";

            using (var xmlTextWriter = XmlWriter.Create(writer, xmlWriterSettings))
            {
                this.WriteXmlOutput(result, xmlTextWriter);
            }
        }

        private static string GetPathOfFirstTestFile(XmlNode resultNode)
        {
            foreach (XmlNode child in resultNode.ChildNodes)
            {
                if (child.Name == "test-suite")
                {
                    return child.GetAttribute("fullname");
                }
            }

            return "UNKNOWN";
        }

        private static string TranslateFailedResult(string label)
        {
            switch (label)
            {
                case "Error":
                case "Cancelled":
                    {
                        return label;
                    }

                default:
                    {
                        return "Failure";
                    }
            }
        }

        private static string TranslateResult(string resultState, string label)
        {
            switch (resultState)
            {
                case "Inconclusive":
                    {
                        return "Inconclusive";
                    }

                case "Failed":
                    {
                        return TranslateFailedResult(label);
                    }

                case "Skipped":
                    {
                        return TranslateSkippedResult(label);
                    }

                case "Passed":
                default:
                    {
                        return "Success";
                    }
            }
        }

        private static string TranslateSkippedResult(string label)
        {
            switch (label)
            {
                case "Ignored":
                    {
                        return "Ignored";
                    }

                case "Invalid":
                    {
                        return "NotRunnable";
                    }

                default:
                    {
                        return "Skipped";
                    }
            }
        }

        private void InitializeXmlFile(XmlNode result)
        {
            var summary = new NUnit2ResultSummary(result);

            this.xmlWriter.WriteStartDocument(false);
            this.xmlWriter.WriteComment("This file represents the results of running a test suite");
            this.xmlWriter.WriteStartElement("test-results");
            this.xmlWriter.WriteAttributeString("name", GetPathOfFirstTestFile(result));
            this.xmlWriter.WriteAttributeString("total", summary.ResultCount.ToString());
            this.xmlWriter.WriteAttributeString("errors", summary.Errors.ToString());
            this.xmlWriter.WriteAttributeString("failures", summary.Failures.ToString());
            this.xmlWriter.WriteAttributeString("not-run", summary.TestsNotRun.ToString());
            this.xmlWriter.WriteAttributeString("inconclusive", summary.Inconclusive.ToString());
            this.xmlWriter.WriteAttributeString("ignored", summary.Ignored.ToString());
            this.xmlWriter.WriteAttributeString("skipped", summary.Skipped.ToString());
            this.xmlWriter.WriteAttributeString("invalid", summary.NotRunnable.ToString());

            var start = result.GetAttribute("start-time", DateTime.UtcNow);
            this.xmlWriter.WriteAttributeString("date", start.ToString("yyyy-MM-dd"));
            this.xmlWriter.WriteAttributeString("time", start.ToString("HH:mm:ss"));
            this.WriteEnvironment();
            this.WriteCultureInfo();
        }

        private void StartTestElement(XmlNode result)
        {
            if (result.Name == "test-case")
            {
                this.xmlWriter.WriteStartElement("test-case");
                this.xmlWriter.WriteAttributeString("name", result.GetAttribute("fullname"));
            }
            else
            {
                var suiteType = result.GetAttribute("type");
                this.xmlWriter.WriteStartElement("test-suite");
                this.xmlWriter.WriteAttributeString(
                    "type",
                    suiteType == "ParameterizedMethod" ? "ParameterizedTest" : suiteType);
                var nameAttr = suiteType == "Assembly" || suiteType == "Project" ? "fullname" : "name";
                this.xmlWriter.WriteAttributeString("name", result.GetAttribute(nameAttr));
            }

            var descNode = result.SelectSingleNode("properties/property[@name='Description']");
            var description = descNode?.GetAttribute("value");
            if (description != null)
            {
                this.xmlWriter.WriteAttributeString("description", description);
            }

            var resultState = result.GetAttribute("result");
            var label = result.GetAttribute("label");
            var executed = resultState == "Skipped" ? "False" : "True";
            var success = resultState == "Passed" ? "True" : "False";

            var duration = result.GetAttribute("duration", 0.0);
            var asserts = result.GetAttribute("asserts");

            this.xmlWriter.WriteAttributeString("executed", executed);
            this.xmlWriter.WriteAttributeString("result", TranslateResult(resultState, label));

            if (!executed.ToUpperInvariant().Equals("TRUE"))
            {
                return;
            }

            this.xmlWriter.WriteAttributeString("success", success);
            this.xmlWriter.WriteAttributeString("time", duration.ToString("#####0.000", NumberFormatInfo.InvariantInfo));
            this.xmlWriter.WriteAttributeString("asserts", asserts);
        }

        private void TerminateXmlFile()
        {
            this.xmlWriter.WriteEndElement(); // test-results
            this.xmlWriter.WriteEndDocument();
            this.xmlWriter.Flush();
            this.xmlWriter.Dispose();
        }

        private void WriteCategoriesElement(XmlNode properties)
        {
            var items = properties.SelectNodes("property[@name='Category']");
            if (items == null || items.Count == 0)
            {
                return; // No category properties found
            }

            this.xmlWriter.WriteStartElement("categories");
            foreach (XmlNode item in items)
            {
                this.xmlWriter.WriteStartElement("category");
                this.xmlWriter.WriteAttributeString("name", item.GetAttribute("value"));
                this.xmlWriter.WriteEndElement();
            }

            this.xmlWriter.WriteEndElement();
        }

        private void WriteCData(string text)
        {
            var start = 0;
            while (true)
            {
                var illegal = text.IndexOf("]]>", start, StringComparison.Ordinal);
                if (illegal < 0)
                {
                    break;
                }

                this.xmlWriter.WriteCData(text.Substring(start, illegal - start + 2));
                start = illegal + 2;
                if (start >= text.Length)
                {
                    return;
                }
            }

            this.xmlWriter.WriteCData(start > 0 ? text.Substring(start) : text);
        }

        private void WriteChildResults(XmlNode result)
        {
            this.xmlWriter.WriteStartElement("results");

            foreach (XmlNode childResult in result.ChildNodes)
            {
                if (childResult.Name.StartsWith("test-"))
                {
                    this.WriteResultElement(childResult);
                }
            }

            this.xmlWriter.WriteEndElement();
        }

        private void WriteCultureInfo()
        {
            this.xmlWriter.WriteStartElement("culture-info");
            this.xmlWriter.WriteAttributeString("current-culture", CultureInfo.CurrentCulture.ToString());
            this.xmlWriter.WriteAttributeString("current-uiculture", CultureInfo.CurrentUICulture.ToString());
            this.xmlWriter.WriteEndElement();
        }

        private void WriteEnvironment()
        {
            this.xmlWriter.WriteStartElement("environment");
            this.xmlWriter.WriteAttributeString(
                "nunit-version",
                this.GetType().GetTypeInfo().Assembly.GetName().Version.ToString());
            this.xmlWriter.WriteAttributeString("clr-version", string.Empty);
            this.xmlWriter.WriteAttributeString("os-version", RuntimeEnvironment.OperatingSystemVersion);
            this.xmlWriter.WriteAttributeString("platform", RuntimeEnvironment.OperatingSystemPlatform.ToString());
            this.xmlWriter.WriteAttributeString("cwd", string.Empty);
            this.xmlWriter.WriteAttributeString("machine-name", Environment.MachineName);
            this.xmlWriter.WriteAttributeString("user", string.Empty);
            this.xmlWriter.WriteAttributeString("user-domain", string.Empty);
            this.xmlWriter.WriteEndElement();
        }

        private void WriteFailureElement(string message, string stackTrace)
        {
            this.xmlWriter.WriteStartElement("failure");
            this.xmlWriter.WriteStartElement("message");
            this.WriteCData(message);
            this.xmlWriter.WriteEndElement();
            this.xmlWriter.WriteStartElement("stack-trace");
            if (stackTrace != null)
            {
                this.WriteCData(stackTrace);
            }

            this.xmlWriter.WriteEndElement();
            this.xmlWriter.WriteEndElement();
        }

        private void WritePropertiesElement(XmlNode properties)
        {
            var items = properties.SelectNodes("property");
            var categories = properties.SelectNodes("property[@name='Category']");
            if (items == null || categories == null || items.Count == categories.Count)
            {
                return; // No non-category properties found
            }

            this.xmlWriter.WriteStartElement("properties");
            foreach (XmlNode item in items)
            {
                if (item.GetAttribute("name") == "Category")
                {
                    continue;
                }

                this.xmlWriter.WriteStartElement("property");
                this.xmlWriter.WriteAttributeString("name", item.GetAttribute("name"));
                this.xmlWriter.WriteAttributeString("value", item.GetAttribute("value"));
                this.xmlWriter.WriteEndElement();
            }

            this.xmlWriter.WriteEndElement();
        }

        private void WriteReasonElement(string message)
        {
            this.xmlWriter.WriteStartElement("reason");
            this.xmlWriter.WriteStartElement("message");
            this.WriteCData(message);
            this.xmlWriter.WriteEndElement();
            this.xmlWriter.WriteEndElement();
        }

        private void WriteResultElement(XmlNode result)
        {
            this.StartTestElement(result);

            var properties = result.SelectSingleNode("properties");
            if (properties != null)
            {
                this.WriteCategoriesElement(properties);
                this.WritePropertiesElement(properties);
            }

            var message = result.SelectSingleNode("reason/message");
            if (message != null)
            {
                this.WriteReasonElement(message.InnerText);
            }

            message = result.SelectSingleNode("failure/message");
            var stackTrace = result.SelectSingleNode("failure/stack-trace");
            if (message != null)
            {
                this.WriteFailureElement(message.InnerText, stackTrace?.InnerText);
            }

            if (result.Name != "test-case")
            {
                this.WriteChildResults(result);
            }

            this.xmlWriter.WriteEndElement(); // test element
        }

        private void WriteXmlOutput(XmlNode result, XmlWriter xmlTextWriter)
        {
            this.xmlWriter = xmlTextWriter;

            this.InitializeXmlFile(result);

            foreach (XmlNode child in result.ChildNodes)
            {
                if (child.Name.StartsWith("test-"))
                {
                    this.WriteResultElement(child);
                }
            }

            this.TerminateXmlFile();
        }
    }
}