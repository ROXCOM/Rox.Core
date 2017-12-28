using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;

namespace Rox.Core
{
    public static class HostigServiceLoader
    {
        public static IHostBuilder UseHostedServices(this IHostBuilder builder, Type type, ILogger logger = null)
        {
            var serviceType = typeof(IHostedService);
            try
            {
                // если интерфейс реализован явно, то класс добавляется как реализация интерфейса IHostedService
                if (serviceType.IsAssignableFrom(type))
                {
                    logger?.LogInformation(string.Format(resource.info_UseHostedServices_typeAssignableInterface, type.FullName, serviceType.FullName));
                    builder.ConfigureServices(services => services.AddSingleton(serviceType, type));
                }
                else
                {
                    logger?.LogInformation(string.Format(resource.info_UseHostedServices_typeNotAssignableInterface, type.FullName, serviceType.FullName));

                    var methodInfos = type.SearchHostedServiceCandidateMethods();
                    if (methodInfos.IsCandidateMethods())
                    {
                        var startMethod = new StartBuilder(methodInfos.Item1);
                        var stopMethod = new StopBuilder(methodInfos.Item2);

                        // иначе необходимо произвести конвертацию найденного класса в класс реализующий интерфейс IHostedService
                        builder.ConfigureServices(services => services.AddSingleton(serviceType, sp =>
                        {
                            var instance = ActivatorUtilities.GetServiceOrCreateInstance(sp, type);
                            var methods = new HostingServiceMethods(instance, startMethod.Build(instance, sp), stopMethod.Build(instance, sp));
                            return new ConventionBasedHostingService(methods);
                        }));
                    }
                    else
                    {
                        logger?.LogInformation(string.Format(resource.info_UseHostedServices_notFoundSutibleMethods, serviceType.FullName));
                    }
                }
            }
            catch (Exception ex)
            {
                var capture = ExceptionDispatchInfo.Capture(ex);
                builder.ConfigureServices(services => services.AddSingleton(serviceType, _ =>
                {
                    capture.Throw();
                    return null;
                }));
            }

            return builder;
        }

        public static IHostBuilder UseHostedServices<T>(this IHostBuilder builder, ILogger logger = null)
        {
            return builder.UseHostedServices(typeof(T), logger: logger);
        }

        public static IHostBuilder UseHostedServices(this IHostBuilder builder, Assembly assembly, ILogger logger = null)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            try
            {
                logger?.LogInformation(string.Format(resource.info_UseHostedServices_startScannAssembly, assembly.FullName));

                var isCandidateAssembly = false;
                var types = new List<Type>();
                foreach(var type in assembly.SearchCandidateTypes())
                {
                    types.Add(type);
                    isCandidateAssembly = true;
                }

                if (isCandidateAssembly)
                {
                    assembly.LoadDependencies();
                    foreach(var type in types)
                        builder.UseHostedServices(type);

                    return builder;
                }
            }
            catch (Exception ex)
            {
                var capture = ExceptionDispatchInfo.Capture(ex);
                logger?.LogError(string.Format(resource.error_UseHostedServices_failedScannAssembly, assembly.FullName, capture.SourceException.Message));
                capture.Throw(); 
            }

            return builder;
        }

        private static Assembly Resolving(AssemblyLoadContext context, AssemblyName assemblyName, RoxAssemblyResolver resolver, ILogger logger)
        {
            logger?.LogInformation($"Find path '{assemblyName.FullName}'");
            string assemblyPath = resolver.GetAssemblyPath(assemblyName);

            if (!string.IsNullOrWhiteSpace(assemblyPath))
            {
                try
                {
                    logger?.LogDebug($"Trying to load from '{assemblyPath}'");
                    var assembly = context.LoadFromAssemblyPath(assemblyPath);

                    if (assembly != null)
                    {
                        logger?.LogDebug($"Successfully resolved assembly to '{assemblyPath}'");
                        return assembly;
                    }
                }

                catch (Exception e)
                {
                    logger?.LogDebug($"Error trying to load '{assemblyName.FullName}': {e.Message}{Environment.NewLine}{e.StackTrace}");

                    if (e.InnerException != null)
                    {
                        logger?.LogDebug($"Inner exception: {e.InnerException.Message}{Environment.NewLine}{e.InnerException.StackTrace}");
                    }
                }
            }
            else
            {
                logger?.LogError($"This '{assemblyName.FullName}' is not found");
            }

            return null;
        }

        private static void LoadDependencies(this Assembly assembly)
        {
            var referecnes = assembly.GetReferencedAssemblies();
            var info = new FileInfo(assembly.Location);
            var resolver = new RoxAssemblyResolver(new RoxRuntimeEnvironment(
                ApplicationDirectory: info.DirectoryName,
                DependencyManifestFile: Path.Combine(info.DirectoryName, assembly.GetName().Name) + ".deps.json"
            ));

            var context = AssemblyLoadContext.GetLoadContext(assembly);
            context.Resolving += (c, n) => Resolving(c, n, resolver, logger: null);

            foreach (var reference in referecnes)
            {
                context.LoadFromAssemblyName(reference);
            }
        }
        
        public static IHostBuilder UseHostedServices(this IHostBuilder builder, IFileInfo info, Func<Assembly, bool> filter = null, ILogger logger = null)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            logger?.LogDebug(string.Format(resource.debug_UseHostedServices_assemblyFileInfo, info.Name, info.PhysicalPath, info.IsDirectory));

            if (info.Exists && !info.IsDirectory)
            {
                logger?.LogDebug(string.Format(resource.debug_UseHostedServices_assemblyFileInfo, info.Name, info.PhysicalPath));
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(info.PhysicalPath);
                if (assembly.IsSystemLibrary() || (filter != null && !filter.Invoke(assembly)))
                {
                    logger?.LogInformation(string.Format(resource.info_UseHostedServices_assembliesFiltered, assembly.FullName));
                    return builder;
                }

                try
                {
                    builder.UseHostedServices(assembly, logger: logger);
                    logger?.LogInformation(string.Format(resource.info_UseHostedServices_assemblyLoadSuccess, info.Name, info.PhysicalPath));
                }
                catch (BadImageFormatException)
                {
                    logger?.LogWarning(string.Format(resource.warning_UseHostedServices_exists, info.Name, info.PhysicalPath));
                }
                catch(Exception e)
                {
                    throw new InvalidOperationException(string.Format(resource.error_UseHostedServices_exceptionLoadAssembly, info.Name, info.PhysicalPath, e.Message, e.GetType().FullName), e);
                }
            }
            else
            {
                if (!info.Exists)
                    logger?.LogDebug(string.Format(resource.warning_UseHostedServices_exists, info.Name, info.PhysicalPath));
            }

            return builder;
        }

        public static IHostBuilder UseHostedServices(this IHostBuilder builder, IFileProvider fileProvider, string directoryOrFilePath = "", bool includeSubfolder = true, Func<IFileInfo, bool> filterInfo = null, Func<Assembly, bool> filterAssembly = null, ILogger logger = null)
        {
            if (fileProvider == null)
                throw new ArgumentNullException(nameof(fileProvider));

            var contents = fileProvider.GetDirectoryContents(directoryOrFilePath);
            if (!contents.Exists)
            {
                logger?.LogError(string.Format(resource.error_UseHostedServices_directoryOrFileNotFound, directoryOrFilePath, fileProvider.GetType().FullName));
                return builder;
            }

            logger?.LogInformation(string.Format(resource.info_UseHostedServices_scaningPath, directoryOrFilePath, includeSubfolder));
            foreach (var content in contents)
            {
                try
                {
                    if (content == null)
                        throw new ArgumentNullException(nameof(content));

                    if (filterInfo != null && !filterInfo.Invoke(content))
                    {
                        logger?.LogInformation(string.Format(resource.info_UseHostedServices_fileIsFiltered, content.Name, content.PhysicalPath));
                        continue;
                    }

                    if (includeSubfolder && content.IsDirectory)
                    {
                        var subPath = Path.Combine(directoryOrFilePath, content.Name);
                        logger?.LogInformation(resource.info_UseHostedServices_scaningSubPath, subPath);
                        builder.UseHostedServices(fileProvider, subPath, includeSubfolder: includeSubfolder, filterInfo: filterInfo, filterAssembly: filterAssembly, logger: logger);
                    }
                    else
                        builder.UseHostedServices(content, filter: filterAssembly, logger: logger);
                }
                catch(Exception e)
                {
                    logger?.LogError(string.Format(resource.error_UseHostedServices_failedFileScan, content.Name, content.PhysicalPath, e.Message));
                }
            };
            logger?.LogInformation(string.Format(resource.info_UseHostedServices_stopScaningPath, directoryOrFilePath));

            return builder;
        }
    }
}