namespace NUnit.Engine.Listeners
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;

    [SuppressMessage("ReSharper", "UseNameofExpression")]
    internal class ServiceMessageWriter
    {
        private const string Header = "##teamcity[";
        private const string Footer = "]";        
                
        public void Write(TextWriter writer, ServiceMessage serviceMessage)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            
            writer.Write(Header);
            writer.Write(serviceMessage.Name);            
            foreach (var attribute in serviceMessage.Attributes)
            {
                writer.Write(' ');
                Write(writer, attribute);                
            }

            writer.Write(Footer);
        }
    

        private void Write(TextWriter writer, ServiceMessageAttr attribute)
        {
            writer.Write(attribute.Name);
            writer.Write("='");
            writer.Write(EscapeString(attribute.Value));
            writer.Write('\'');
        }

        private static string EscapeString(string value)
        {
            return value != null
                ? value.Replace("|", "||")
                       .Replace("'", "|'")
                       .Replace("\n", "|n")
                       .Replace("\r", "|r")
                       .Replace(char.ConvertFromUtf32(int.Parse("0086", NumberStyles.HexNumber)), "|x")
                       .Replace(char.ConvertFromUtf32(int.Parse("2028", NumberStyles.HexNumber)), "|l")
                       .Replace(char.ConvertFromUtf32(int.Parse("2029", NumberStyles.HexNumber)), "|p")
                       .Replace("[", "|[")
                       .Replace("]", "|]")
                : null;
        }
    }
}
