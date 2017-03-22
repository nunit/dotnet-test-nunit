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
using System.Text;
using NUnit.Runner;

namespace NUnit.Adapter
{
    internal class Console: IConsole, IDisposable
    {
        private readonly Action<string> _consoleWriter;
        private readonly StringBuilder _buffer = new StringBuilder();

        public Console(Action<string> consoleWriter)
        {
            if (consoleWriter == null) throw new ArgumentNullException(nameof(consoleWriter));
            _consoleWriter = consoleWriter;
        }

        public void Write(ColorStyle color, string text)
        {
            _buffer.Append(text ?? string.Empty);
        }

        public void WriteLine(string text = null)
        {
            _buffer.AppendLine(text ?? string.Empty);
            Flush();
        }

        public void WriteLine(ColorStyle style, string text)
        {
            WriteLine(text);
        }

        public void WriteLabel(string label, object option, ColorStyle valueStyle = ColorStyle.Value)
        {
            _buffer.Append($"{label}{option}");
        }

        public void WriteLabelLine(string label, object option, ColorStyle valueStyle = ColorStyle.Value)
        {
            WriteLine($"{label}{option}");
        }

        public void Dispose()
        {
            Flush();
        }

        private void Flush()
        {
            if (_buffer.Length > 0)
            {
                var message = _buffer.ToString().TrimEnd();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    _consoleWriter(message);
                }

                _buffer.Length = 0;
            }
        }
    }
}
