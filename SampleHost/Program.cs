﻿using System;
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
            //ReflectionHelper.ShowReferences();
            var builder = new HostBuilder()
                .UseConsoleLifetime()
                .UseHostedServices(
                    new PhysicalFileProvider(Directory.GetCurrentDirectory())
                    , filterAssembly: assembly => assembly.RegexPattern("Sample.*")
                );

            await builder.RunConsoleAsync();

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}