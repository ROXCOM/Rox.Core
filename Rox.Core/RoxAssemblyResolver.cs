using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyModel;
using Microsoft.DotNet.InternalAbstractions;

namespace Rox.Core
{
    public class RoxRuntimeEnvironment
    {
        public RoxRuntimeEnvironment(string ApplicationDirectory, string DependencyManifestFile)
        {
            this.ApplicationDirectory = ApplicationDirectory;
            this.DependencyManifestFile = DependencyManifestFile;
        }

        public string ApplicationDirectory
        {
            get;
        }

        public string DependencyManifestFile
        {
            get;
        }
    }

    public class RoxAssemblyResolver
    {
        internal readonly Dictionary<string, string> CompileAssemblies = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _libraries = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _nativeLibraries = new Dictionary<string, string>();
        private readonly string _packagesPath;
        private ILogger logger = null;
        private RoxRuntimeEnvironment runtimeEnvironment;

        public RoxAssemblyResolver(RoxRuntimeEnvironment runtimeEnvironment)
        {
            logger?.LogDebug("Starting");
            logger?.LogDebug("Getting the packages path");

            this.runtimeEnvironment = runtimeEnvironment;

            _packagesPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

            if (String.IsNullOrEmpty(_packagesPath))
            {
                string profileDirectory = Environment.GetEnvironmentVariable("USERPROFILE");

                if (String.IsNullOrEmpty(profileDirectory))
                {
                    profileDirectory = Environment.GetEnvironmentVariable("HOME");
                }

                _packagesPath = Path.Combine(profileDirectory, ".nuget", "packages");
            }

            LoadDependencyManifest(runtimeEnvironment.DependencyManifestFile);

            logger?.LogDebug("Packages path is {0}", _packagesPath);
            logger?.LogDebug("Finished");
        }

        public void LoadDependencyManifest(string dependencyManifestFile)
        {
            logger?.LogDebug("Loading dependency manifest from {0}", dependencyManifestFile);

            var dependencyContextReader = new DependencyContextJsonReader();

            using (var dependencyManifestStream = new FileStream(dependencyManifestFile, FileMode.Open, FileAccess.Read))
            {
                logger?.LogDebug("Reading dependency manifest file and merging in dependencies from the shared runtime");
                var dependencyContext = dependencyContextReader.Read(dependencyManifestStream);

                var runtimeDependencyManifestFile = (string)AppContext.GetData("FX_DEPS_FILE");

                if (!string.IsNullOrEmpty(runtimeDependencyManifestFile) && runtimeDependencyManifestFile != dependencyManifestFile)
                {
                    logger?.LogDebug("Merging in the dependency manifest from the shared runtime at {0}", runtimeDependencyManifestFile);

                    using (var runtimeDependencyManifestStream = new FileStream(runtimeDependencyManifestFile, FileMode.Open, FileAccess.Read))
                    {
                        dependencyContext = dependencyContext.Merge(dependencyContextReader.Read(runtimeDependencyManifestStream));
                    }
                }

                AddDependencies(dependencyContext);
            }

            var entryAssemblyPath = dependencyManifestFile.Replace(".deps.json", ".dll");

            if (File.Exists(entryAssemblyPath))
            {
                Assembly entryAssembly = Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(entryAssemblyPath)));
                Lazy<DependencyContext> defaultDependencyContext = new Lazy<DependencyContext>(() => DependencyContext.Load(entryAssembly));

                // I really don't like doing it this way, but it's the easiest way to give the running code access to the default 
                // dependency context data
                typeof(DependencyContext).GetField("_defaultContext", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, defaultDependencyContext);
            }

            logger?.LogDebug("Finished");
        }

        private void AddDependencies(DependencyContext dependencyContext)
        {
            logger?.LogDebug("Adding dependencies for {0}", dependencyContext.Target.Framework);

            foreach (var compileLibrary in dependencyContext.CompileLibraries)
            {
                if (compileLibrary.Assemblies == null || compileLibrary.Assemblies.Count == 0)
                {
                    continue;
                }

                logger?.LogDebug("Processing compile dependency {0}", compileLibrary.Name);

                // standalone
                string assemblyPath = File.Exists(Path.Combine(runtimeEnvironment.ApplicationDirectory, "refs", Path.GetFileName(compileLibrary.Assemblies[0].Replace('/', Path.DirectorySeparatorChar))))
                    ? Path.Combine(runtimeEnvironment.ApplicationDirectory, "refs", Path.GetFileName(compileLibrary.Assemblies[0].Replace('/', Path.DirectorySeparatorChar)))
                    : Path.Combine(_packagesPath, compileLibrary.Name, compileLibrary.Version, compileLibrary.Assemblies[0].Replace('/', Path.DirectorySeparatorChar));

                if (!CompileAssemblies.ContainsKey(compileLibrary.Name))
                {
                    if (File.Exists(assemblyPath))
                    {
                        CompileAssemblies[compileLibrary.Name] = assemblyPath;
                        logger?.LogDebug("EdgeAssemblyResolver:а:AddDependencies (CLR) - Added compile assembly {0}", assemblyPath);
                    }
                }
                else
                {

                    logger?.LogDebug("Already present in the compile assemblies list, skipping");
                }
            }

            var supplementaryRuntimeLibraries = new Dictionary<string, string>();

            foreach (var runtimeLibrary in dependencyContext.RuntimeLibraries)
            {
                logger?.LogDebug("Processing runtime dependency {1} {0}", runtimeLibrary.Name, runtimeLibrary.Type);

                List<string> assets = runtimeLibrary.RuntimeAssemblyGroups.GetRuntimeAssets(RuntimeEnvironment.GetRuntimeIdentifier()).ToList();

                if (!assets.Any())
                {
                    assets = runtimeLibrary.RuntimeAssemblyGroups.GetDefaultAssets().ToList();
                }

                if (assets.Any())
                {
                    string assetPath = assets[0];
                    string assemblyPath = runtimeLibrary.Type == "project"
                        ? Path.Combine(runtimeEnvironment.ApplicationDirectory, assetPath)
                        : Path.Combine(runtimeEnvironment.ApplicationDirectory, Path.GetFileName(assetPath));

                    string libraryNameFromPath = Path.GetFileNameWithoutExtension(assemblyPath);

                    if (!File.Exists(assemblyPath))
                    {
                        assemblyPath = runtimeLibrary.Type == "project"
                            ? Path.Combine(runtimeEnvironment.ApplicationDirectory, assetPath)
                            : Path.Combine(_packagesPath, runtimeLibrary.Name, runtimeLibrary.Version, assetPath.Replace('/', Path.DirectorySeparatorChar));
                    }

                    if (!_libraries.ContainsKey(runtimeLibrary.Name))
                    {
                        _libraries[runtimeLibrary.Name] = assemblyPath;
                        logger?.LogDebug("Added runtime assembly {0}", assemblyPath);
                    }

                    else
                    {
                        logger?.LogDebug("Already present in the runtime assemblies list, skipping");
                    }

                    if (runtimeLibrary.Name != libraryNameFromPath && !_libraries.ContainsKey(libraryNameFromPath))
                    {
                        supplementaryRuntimeLibraries[libraryNameFromPath] = assemblyPath;
                    }

                    if (!CompileAssemblies.ContainsKey(runtimeLibrary.Name))
                    {
                        CompileAssemblies[runtimeLibrary.Name] = assemblyPath;
                        logger?.LogDebug("Added compile assembly {0}", assemblyPath);
                    }

                    else
                    {
                        logger?.LogDebug("Already present in the compile assemblies list, skipping");
                    }
                }

                foreach (string libraryName in supplementaryRuntimeLibraries.Keys)
                {
                    if (!_libraries.ContainsKey(libraryName))
                    {
                        logger?.LogDebug(
                            "Filename in the dependency context did not match the package/project name, added additional resolver for {0}",
                            libraryName);
                        _libraries[libraryName] = supplementaryRuntimeLibraries[libraryName];
                    }

                    if (!CompileAssemblies.ContainsKey(libraryName))
                    {
                        CompileAssemblies[libraryName] = supplementaryRuntimeLibraries[libraryName];
                    }
                }

                List<string> nativeAssemblies = runtimeLibrary.GetRuntimeNativeAssets(dependencyContext, RuntimeEnvironment.GetRuntimeIdentifier()).ToList();

                if (nativeAssemblies.Any())
                {
                    logger?.LogDebug("Adding native dependencies for {0}", RuntimeEnvironment.GetRuntimeIdentifier());

                    foreach (string nativeAssembly in nativeAssemblies)
                    {
                        string nativeAssemblyPath = Path.Combine(_packagesPath, runtimeLibrary.Name, runtimeLibrary.Version, nativeAssembly.Replace('/', Path.DirectorySeparatorChar));

                        logger?.LogDebug("Adding native assembly {0} at {1}",
                            Path.GetFileNameWithoutExtension(nativeAssembly), nativeAssemblyPath);
                        _nativeLibraries[Path.GetFileNameWithoutExtension(nativeAssembly)] = nativeAssemblyPath;
                    }
                }
            }
        }

        public string GetAssemblyPath(AssemblyName assemblyName)
        {
            if (!_libraries.ContainsKey(assemblyName.Name))
            {
                return null;
            }

            return _libraries[assemblyName.Name];
        }

        public string GetNativeLibraryPath(string libraryName)
        {
            if (!_nativeLibraries.ContainsKey(libraryName))
            {
                return null;
            }

            return _nativeLibraries[libraryName];
        }

        internal void AddCompiler(string bootstrapDependencyManifest)
        {
            logger?.LogDebug("Adding the compiler from dependency manifest file {0}", bootstrapDependencyManifest);

            DependencyContextJsonReader dependencyContextReader = new DependencyContextJsonReader();

            using (FileStream bootstrapDependencyManifestStream = new FileStream(bootstrapDependencyManifest, FileMode.Open, FileAccess.Read))
            {
                DependencyContext compilerDependencyContext = dependencyContextReader.Read(bootstrapDependencyManifestStream);

                logger?.LogDebug("Adding dependencies for the compiler");
                AddDependencies(compilerDependencyContext);

                logger?.LogDebug("Finished");
            }
        }
    }
}
