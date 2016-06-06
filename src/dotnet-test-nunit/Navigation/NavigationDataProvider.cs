// ****************************************************************
// Copyright (c) 2016 NUnit Software. All rights reserved.
// ****************************************************************

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.Testing.Abstractions;

namespace NUnit.Runner.Navigation
{
    public class NavigationDataProvider
    {
        public static NavigationDataProvider GetNavigationDataProvider(string assemblyPath)
        {
            var directory = Path.GetDirectoryName(assemblyPath);
            var assembly = Path.GetFileNameWithoutExtension(assemblyPath);
            var pdbPath = Path.Combine(directory, assembly + FileNameSuffixes.DotNet.ProgramDatabase);

            if (!File.Exists(pdbPath)) return null;

            return new NavigationDataProvider(assemblyPath, pdbPath);
        }

        ISourceInformationProvider _provider;
        Assembly _assembly;

        NavigationDataProvider(string assembyPath, string pdbPath)
            : this(assembyPath, new SourceInformationProvider(pdbPath))
        {
        }

        NavigationDataProvider(string assemblyPath, ISourceInformationProvider provider)
        {
            _assembly = LoadAssembly(assemblyPath);
            _provider = provider;
        }

        public SourceInformation GetSourceData(string className, string methodName)
        {
            var type = _assembly.DefinedTypes.FirstOrDefault(t => t.FullName == className);
            var method = type?.DeclaredMethods.FirstOrDefault(m => m.Name == methodName);
            if (method == null) return null;

            return _provider.GetSourceInformation(method);
        }

        static Assembly LoadAssembly(string assemblyPath) =>
#if NET451
            Assembly.LoadFrom(assemblyPath);
#else
            System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
#endif
    }
}
