using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;

namespace Rox.Core
{
    public interface IAssemblyResolver
    {
        Assembly Resolving(AssemblyLoadContext context, AssemblyName assemblyName);
    }
    public class PhysicalAssemblyResolver : IAssemblyResolver
    {
        private IEnumerable<Func<AssemblyName, string>> paths = new List<Func<AssemblyName, string>>()
        {
            (assemblyName) => Path.Combine(Directory.GetCurrentDirectory(), assemblyName.Name, (assemblyName.Version?.ToString() ?? "")),
            (assemblyName) => Path.Combine(Directory.GetCurrentDirectory(), assemblyName.Name),
            (assemblyName) => Path.Combine(Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? "", assemblyName.Name, (assemblyName.Version?.ToString() ?? "")),

            (assemblyName) => Path.Combine(Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? "", assemblyName.Name, (assemblyName.Version?.ToString() ?? "")),
            (assemblyName) => Path.Combine(Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? "", assemblyName.Name),
            (assemblyName) => Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".nuget", "packages", assemblyName.Name, (assemblyName.Version?.ToString() ?? "")),
            (assemblyName) => Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".nuget", "packages", assemblyName.Name),
            (assemblyName) => Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "", ".nuget", "packages", assemblyName.Name, (assemblyName.Version?.ToString() ?? "")),
            (assemblyName) => Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "", ".nuget", "packages", assemblyName.Name)
        };
        public IEnumerable<Func<AssemblyName, string>> Paths => paths;

        public Assembly Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            foreach (var pathFunc in paths)
            {
                var path = pathFunc(assemblyName);
                if (File.Exists(path))
                {
                    var assembly = context.LoadFromAssemblyPath(path);
                    if (assembly != null)
                        return assembly;
                }
            }

            return null;
        }
    }

    public static class HostigServiceLoader
    {
        private static IAssemblyResolver resolver = new PhysicalAssemblyResolver();

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

            var name = assembly.ManifestModule.Name;

            try
            {
                logger?.LogInformation(string.Format(resource.info_UseHostedServices_startScannAssembly, name));

                var isCandidateAssembly = false;
                //Parallel.ForEach(assembly.SearchHostedServiceCandidateTypes(), type =>
                //{
                //    builder.UseHostedServices(type);
                //    isCandidateAssembly = true;
                //});

                foreach(var type in assembly.SearchCandidateTypes())
                {
                    builder.UseHostedServices(type);
                    isCandidateAssembly = true;

                    if (isCandidateAssembly) break;
                }

                if (isCandidateAssembly)
                {
                    var context = AssemblyLoadContext.GetLoadContext(assembly);
                    foreach(var reference in assembly.GetReferencedAssemblies())
                    {
                        var depended = resolver.Resolving(context, reference);
                        if (depended == null)
                        {
                            logger?.LogError($"assembly {reference.FullName} not found");
                        }
                        //var path = Path.Combine(Directory.GetCurrentDirectory(), reference.Name);
                        //context.LoadFromAssemblyPath(path);
                    }
                }
            }
            catch (Exception ex)
            {
                var capture = ExceptionDispatchInfo.Capture(ex);
                logger?.LogError(string.Format(resource.error_UseHostedServices_failedScannAssembly, name, capture.SourceException.Message));
                capture.Throw(); 
            }

            return builder;
        }

        public static IHostBuilder UseHostedServices(this IHostBuilder builder, Stream stream, Func<Assembly, bool> filter = null, ILogger logger = null)
        {
            var context = AssemblyLoadContext.Default;//new IndividualAssemblyLoadContext();
            context.Resolving += resolver.Resolving;
            var assembly = context.LoadFromStream(stream);
            if (assembly.IsSystemLibrary() || (filter != null && !filter.Invoke(assembly)))
            {
                logger?.LogInformation(string.Format(resource.info_UseHostedServices_assembliesFiltered, assembly.FullName));
                return builder;
            }

            return builder.UseHostedServices(assembly);
        }

        public static IHostBuilder UseHostedServices(this IHostBuilder builder, IFileInfo info, Func<Assembly, bool> filter = null, ILogger logger = null)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            logger?.LogDebug(string.Format(resource.debug_UseHostedServices_assemblyFileInfo, info.Name, info.PhysicalPath, info.IsDirectory));

            if (info.Exists && !info.IsDirectory)
            {
                logger?.LogDebug(string.Format(resource.debug_UseHostedServices_assemblyFileInfo, info.Name, info.PhysicalPath));
                using (var stream = info.CreateReadStream())
                {
                    logger?.LogDebug(string.Format(resource.debug_UseHostedServices_assemblyFileInfo, info.Name, info.PhysicalPath));
                    try
                    {
                        builder.UseHostedServices(stream, filter: filter, logger: logger);
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