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

namespace NUnit.Runner
{
    /// <summary>
    /// Env is a static class that provides some of the features of
    /// System.Environment that are not available under all runtimes
    /// </summary>
    public class Env
    {
        static Env()
        {
#if NETSTANDARD1_5 || NETCOREAPP1_0
            DocumentFolder = ".";
            string drive = Environment.GetEnvironmentVariable("HOMEDRIVE");
            string path = Environment.GetEnvironmentVariable("HOMEPATH");
            if (drive != null && path != null)
            {
                DocumentFolder = Path.Combine(drive, path, "documents");
            }
            else if (path != null)
            {
                DocumentFolder = Path.Combine(path, "documents");
            }
            else
            {
                string profile = Environment.GetEnvironmentVariable("USERPROFILE");
                if (profile != null)
                    DocumentFolder = Path.Combine(profile, "documents");
            }
            DefaultWorkDirectory = DocumentFolder;
#else
            DocumentFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            DefaultWorkDirectory = Environment.CurrentDirectory;
#endif
        }

        /// <summary>
        /// Path to the 'My Documents' folder
        /// </summary>
        public static string DocumentFolder;

        /// <summary>
        /// Directory used for file output if not specified on commandline.
        /// </summary>
        public static readonly string DefaultWorkDirectory;
    }
}
