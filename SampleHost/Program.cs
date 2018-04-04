using System;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Rox.Core;

namespace Samples
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .UseHostedServices()
                .UseAssemblies(
                    new PhysicalFileProvider(Directory.GetCurrentDirectory()),
                    filterAssembly: assembly => assembly.RegexPattern("Sample.*")
                )
                .UseConsoleLifetime();

            await builder.RunConsoleAsync();

            //var type = typeof(ConventionBased).CreateType<IHostedService>();

            //var generatedClassObject = Activator.CreateInstance(type, typeof(MyServiceI), null);

            //var hostedService = InterfaceObjectFactory.New<IHostedService>();
            //var type = typeof(ConventionBased).CreateType<IHostedService>();
            //generatedClassObject.GetType().InvokeMember("StartAsync", System.Reflection.BindingFlags.CreateInstance);
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }

    public class MyServiceI
    {
        private bool _stopping;
        private Task _backgroundTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("MyServiceC is starting.");
            _backgroundTask = BackgroundTask();
            return Task.CompletedTask;
        }

        private async Task BackgroundTask()
        {
            while (!_stopping)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                Console.WriteLine("MyServiceC is doing background work.");
            }

            Console.WriteLine("MyServiceC background task is stopping.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("MyServiceC is stopping.");
            _stopping = true;
            if (_backgroundTask != null)
            {
                // TODO: cancellation
                await _backgroundTask;
            }
        }
    }
}