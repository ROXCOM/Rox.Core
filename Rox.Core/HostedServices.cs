using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace Rox.Core
{
    public interface IHostedServicesCandidates
    {
        //IEnumerable<Type> CandidateTypes();
        IEnumerable<IHostedService> Create();
    }

    public class HostedServices : ConcurrentDictionary<int, IHostedService>, IHostedService
    {
        public IList<IHostedServicesCandidates> Templates { get; }
        private ILogger logger;
        private List<IHostedService> services;
        private Timer timer = new Timer(Timer);

        public HostedServices(IEnumerable<IHostedServicesCandidates> templates, ILoggerFactory loggerFactory)
        {
            this.services = new List<IHostedService>();
            this.Templates = templates.ToList();
            this.logger = loggerFactory.CreateLogger<HostedServices>();
            this.timer = new Timer(Timer, this.services, 0, 5000);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var service in this.services)
            {
                try
                {
                    await service.StartAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError($"service '{service.ToString()}' not started because error '{e.Message}'");
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var service in this.services)
            {
                try
                {
                    await service.StopAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError($"service '{service.ToString()}' not started because service reported an error on 'Start' method '{e.Message}'");
                }
                
            }
        }

        private async Task StartAsyncService(IEnumerable<IHostedService> services, CancellationToken tocken = default)
        {
            var processedService = new List<IHostedService>();
            try
            {
                foreach (var service in services)
                {
                    try
                    {
                        await service.StartAsync(tocken);
                        processedService.Add(service);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"service '{service.ToString()}' not started because error '{e.Message}'");
                    }

                    tocken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException e)
            {
                foreach (var service in processedService)
                {

                }
            }
        }

        private async Task Initialize(CancellationToken tocken)
        {
            foreach(var template in Templates)
            {
                this.services.AddRange(template.Create());
            }
        }

        private async Task Uninitialize(CancellationToken tocken)
        {

        }

        private static void Timer(object state)
        {

        }
    }

    public class IncludeTypeRule : IHostedServicesCandidates
    {
        private IEnumerable<Type> types;
        private IServiceProvider provider;

        public IncludeTypeRule(IServiceProvider provider, IEnumerable<Type> types) { this.provider = provider; this.types = types; }
        public IncludeTypeRule(IServiceProvider provider, params Type[] types) { this.provider = provider; this.types = types; }

        public IEnumerable<Type> CandidateTypes()
        {
            return types;
        }

        public IEnumerable<IHostedService> Create()
        {
            var logger = provider.GetService<ILoggerFactory>().CreateLogger<IncludeTypeRule>();
            foreach (var type in types)
            {
                yield return type.CreateOrNull(provider, logger);
            }
        }
    }
    public class IncludeAssemblyRule : IHostedServicesCandidates
    {
        private IEnumerable<Assembly> assemblies;
        private IServiceProvider provider;

        public IncludeAssemblyRule(IServiceProvider provider, IEnumerable<Assembly> assemblies) { this.provider = provider; this.assemblies = assemblies; }
        public IncludeAssemblyRule(IServiceProvider provider, params Assembly[] assemblies) { this.provider = provider; this.assemblies = assemblies; }

        public IEnumerable<IHostedService> Create()
        {
            var logger = provider.GetService<ILoggerFactory>().CreateLogger<IncludeAssemblyRule>();
            var hosteds = new List<IHostedService>();
            foreach (var assembly in assemblies)
            {
                hosteds.AddRange(assembly.CreateOrNull(provider, logger));
            }

            return hosteds;
        }
    }
    public class IncludeFileInfoRule : IHostedServicesCandidates
    {
        private IFileInfo info;
        private IServiceProvider provider;
        private Expression<Func<Assembly, bool>> filter;

        public IncludeFileInfoRule(IServiceProvider provider, IFileInfo info, Expression<Func<Assembly, bool>> filter = null) { this.provider = provider; this.info = info; this.filter = filter; }

        public IEnumerable<IHostedService> Create()
        {
            var logger = provider.GetService<ILoggerFactory>().CreateLogger<IncludeFileInfoRule>();
            return info.CreateOrNull(provider, filter?.Compile(), logger);
        }
    }
    public class IncludeFileProviderRule : IHostedServicesCandidates
    {
        private IServiceProvider provider;
        IFileProvider fileProvider;
        string directoryOrFilePath;
        bool includeSubfolder;
        Expression<Func<IFileInfo, bool>> filterInfo;
        Expression<Func<Assembly, bool>> filterAssembly;

        public IncludeFileProviderRule(
            IServiceProvider provider, 
            IFileProvider fileProvider, 
            string directoryOrFilePath = "", 
            bool includeSubfolder = true, 
            Expression<Func<IFileInfo, bool>> filterInfo = null, 
            Expression<Func<Assembly, bool>> filterAssembly = null)
        {
            this.provider = provider;
            this.fileProvider = fileProvider;
            this.directoryOrFilePath = directoryOrFilePath;
            this.includeSubfolder = includeSubfolder;
            this.filterInfo = filterInfo;
            this.filterAssembly = filterAssembly;
        }

        public IEnumerable<IHostedService> Create()
        {
            var logger = provider.GetService<ILoggerFactory>().CreateLogger<IncludeFileProviderRule>();
            return fileProvider.CreateOrNull(provider, directoryOrFilePath, includeSubfolder, filterInfo?.Compile(), filterAssembly?.Compile(), logger);
        }
    }

    public static class HostedServicesBuilderExtensions
    {
        public static IHostedService CreateOrNull(this Type type, IServiceProvider sp, ILogger logger = null)
        {
            var serviceType = typeof(IHostedService);
            try
            {
                // если интерфейс реализован явно, то класс добавляется как реализация интерфейса IHostedService
                if (serviceType.IsAssignableFrom(type))
                {
                    logger?.LogInformation(string.Format(resource.info_UseHostedServices_typeAssignableInterface, type.FullName, serviceType.FullName));
                    return ActivatorUtilities.GetServiceOrCreateInstance(sp,  type) as IHostedService;
                }
                else
                {
                    logger?.LogInformation(string.Format(resource.info_UseHostedServices_typeNotAssignableInterface, type.FullName, serviceType.FullName));

                    var methodInfos = type.SearchHostedServiceCandidateMethods();
                    if (methodInfos.IsCandidateMethods())
                    {
                        var startMethod = new MethodBuilder(methodInfos.Item1);
                        var stopMethod = new MethodBuilder(methodInfos.Item2);

                        // иначе необходимо произвести конвертацию найденного класса в класс реализующий интерфейс IHostedService
                        var instance = ActivatorUtilities.GetServiceOrCreateInstance(sp, type);
                        var methods = new HostingServiceMethods(instance, startMethod.Build(instance, sp), stopMethod.Build(instance, sp));
                        return new ConventionBasedHostingService(methods);
                    }
                    else
                    {
                        logger?.LogInformation(string.Format(resource.info_UseHostedServices_notFoundSutibleMethods, serviceType.FullName));
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError($"chek type and create instant '{type.FullName}' failed. error '{e.Message}'");
            }
            return null;
        }

        public static IEnumerable<IHostedService> CreateOrNull(this Assembly assembly, IServiceProvider provider, ILogger logger = null)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var services = new List<IHostedService>();
            try
            {
                logger?.LogInformation(string.Format(resource.info_UseHostedServices_startScannAssembly, assembly.FullName));

                var isCandidateAssembly = false;
                var types = new List<Type>();
                foreach (var type in assembly.SearchCandidateTypes())
                {
                    types.Add(type);
                    isCandidateAssembly = true;
                }

                if (isCandidateAssembly)
                {
                    assembly.LoadDependencies();
                    foreach (var type in types)
                        services.Add(type.CreateOrNull(provider, logger));
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(string.Format(resource.error_UseHostedServices_failedScannAssembly, assembly.FullName, ex.Message));
            }

            return services;
        }

        public static IEnumerable<IHostedService> CreateOrNull(this IFileInfo info, IServiceProvider provider, Func<Assembly, bool> filter = null, ILogger logger = null)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            logger?.LogDebug(string.Format(resource.debug_UseHostedServices_assemblyFileInfo, info.Name, info.PhysicalPath, info.IsDirectory));

            var hosteds = new List<IHostedService>();
            if (info.Exists && !info.IsDirectory)
            {
                try
                {
                    logger?.LogDebug(string.Format(resource.debug_UseHostedServices_assemblyFileInfo, info.Name, info.PhysicalPath, info.IsDirectory));
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(info.PhysicalPath);
                    if (assembly.IsSystemLibrary() || (filter != null && !filter.Invoke(assembly)))
                    {
                        logger?.LogInformation(string.Format(resource.info_UseHostedServices_assembliesFiltered, assembly.FullName));
                        return hosteds;
                    }

                    hosteds.AddRange(assembly.CreateOrNull(provider, logger));
                    logger?.LogInformation(string.Format(resource.info_UseHostedServices_assemblyLoadSuccess, info.Name, info.PhysicalPath));
                }
                catch (BadImageFormatException)
                {
                    logger?.LogWarning(string.Format(resource.warning_UseHostedServices_exists, info.Name, info.PhysicalPath));
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(string.Format(resource.error_UseHostedServices_exceptionLoadAssembly, info.Name, info.PhysicalPath, e.Message, e.GetType().FullName), e);
                }
            }
            else
            {
                if (!info.Exists)
                    logger?.LogDebug(string.Format(resource.warning_UseHostedServices_exists, info.Name, info.PhysicalPath));
            }

            return hosteds;
        }

        public static IEnumerable<IHostedService> CreateOrNull(this IFileProvider fileProvider, IServiceProvider provider, string directoryOrFilePath = "", bool includeSubfolder = true, Func<IFileInfo, bool> filterInfo = null, Func<Assembly, bool> filterAssembly = null, ILogger logger = null)
        {
            if (fileProvider == null)
                throw new ArgumentNullException(nameof(fileProvider));

            var hosted = new List<IHostedService>();
            var contents = fileProvider.GetDirectoryContents(directoryOrFilePath);
            if (!contents.Exists)
            {
                logger?.LogError(string.Format(resource.error_UseHostedServices_directoryOrFileNotFound, directoryOrFilePath, fileProvider.GetType().FullName));
                return hosted;
            }

            logger?.LogInformation(string.Format(resource.info_UseHostedServices_scaningPath, directoryOrFilePath, includeSubfolder));
            foreach (var content in contents)
            {
                try
                {
                    if (filterInfo != null && !filterInfo.Invoke(content))
                    {
                        logger?.LogInformation(string.Format(resource.info_UseHostedServices_fileIsFiltered, content.Name, content.PhysicalPath));
                        continue;
                    }

                    if (includeSubfolder && content.IsDirectory)
                    {
                        var subPath = Path.Combine(directoryOrFilePath, content.Name);
                        logger?.LogInformation(resource.info_UseHostedServices_scaningSubPath, subPath);



                        hosted.AddRange(fileProvider.CreateOrNull(provider, subPath, includeSubfolder: includeSubfolder, filterInfo: filterInfo, filterAssembly: filterAssembly, logger: logger));
                    }
                    else
                        hosted.AddRange(content.CreateOrNull(provider, filter: filterAssembly, logger: logger));
                }
                catch (Exception e)
                {
                    logger?.LogError(string.Format(resource.error_UseHostedServices_failedFileScan, content.Name, content.PhysicalPath, e.Message));
                }
            }
            logger?.LogInformation(string.Format(resource.info_UseHostedServices_stopScaningPath, directoryOrFilePath));

            return hosted;
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

        public static IHostBuilder UseHostedServices(this IHostBuilder builder)
        {
            return builder.ConfigureServices(s => s.AddSingleton<IHostedService, HostedServices>());
        }

        public static IHostBuilder UseTypes(this IHostBuilder builder, params Type[] types)
        {
            return builder.ConfigureServices(s => s.AddSingleton<IHostedServicesCandidates>(provider => new IncludeTypeRule(provider, types)));
        }

        public static IHostBuilder UseType(this IHostBuilder builder, Type type)
        {
            return builder.UseTypes(type);
        }

        public static IHostBuilder UseType<T>(this IHostBuilder builder)
        {
            return builder.UseTypes(typeof(T));
        }

        public static IHostBuilder UseAssemblies(this IHostBuilder builder, params Assembly[] assemblies)
        {
            return builder.ConfigureServices(s => s.AddSingleton<IHostedServicesCandidates>(provider => new IncludeAssemblyRule(provider, assemblies)));
        }

        public static IHostBuilder UseAssemblies(this IHostBuilder builder, IFileInfo info, Expression<Func<Assembly, bool>> filter = null)
        {
            return builder.ConfigureServices(s => s.AddSingleton<IHostedServicesCandidates>(provider => new IncludeFileInfoRule(provider, info, filter)));
        }

        public static IHostBuilder UseAssemblies(this IHostBuilder builder, IFileProvider fileProvider, string directoryOrFilePath = "", bool includeSubfolder = true, Expression<Func<IFileInfo, bool>> filterInfo = null, Expression<Func<Assembly, bool>> filterAssembly = null)
        {
            return builder.ConfigureServices(s => s.AddSingleton<IHostedServicesCandidates>(provider => new IncludeFileProviderRule(provider, fileProvider, directoryOrFilePath, includeSubfolder, filterInfo, filterAssembly)));
        }
    }
}