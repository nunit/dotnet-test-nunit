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
using System.IO;

namespace NUnit
{
    /// <summary>
    /// Env is a static class that provides some of the features of
    /// System.Environment that are not available under all runtimes
    /// </summary>
    public class Env
    {
        static Env()
        {
#if NETSTANDARDAPP1_5 || NETCOREAPP1_0
            string drive = Environment.GetEnvironmentVariable("HOMEDRIVE");
            string path = Environment.GetEnvironmentVariable("HOMEPATH");
            DocumentFolder = Path.Combine(drive, path, "documents");
#else
            DocumentFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
#endif
        }

        /// <summary>
        /// Path to the 'My Documents' folder
        /// </summary>
        public static string DocumentFolder;

        /// <summary>
        /// Directory used for file output if not specified on commandline.
        /// </summary>
#if NETSTANDARDAPP1_5 || NETCOREAPP1_0
        public static readonly string DefaultWorkDirectory = DocumentFolder;
#else
        public static readonly string DefaultWorkDirectory = Environment.CurrentDirectory;
#endif
    }
}
