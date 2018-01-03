using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}