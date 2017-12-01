using System;
using System.IO;
using System.Linq;

namespace Rox.Core
{
    //public static class ReflectionHelper
    //{
    //    public static void ShowReferences()
    //    {
    //        var context = DependencyContext.Default;

    //        if (!context.CompileLibraries.Any())
    //            Console.WriteLine("Compilation libraries empty");

    //        foreach (var compilationLibrary in context.CompileLibraries)
    //        {
    //            foreach (var resolvedPath in compilationLibrary
    //                                          .ResolveReferencePaths())
    //            {
    //                Console.WriteLine($"Compilation {compilationLibrary.Name}:{Path.GetFileName(resolvedPath)}");
    //                if (!File.Exists(resolvedPath))
    //                    Console.WriteLine($"Compilation library resolved to non existent path {resolvedPath}");
    //            }
    //        }

    //        foreach (var runtimeLibrary in context.RuntimeLibraries)
    //        {
    //            foreach (var assembly in runtimeLibrary.GetDefaultAssemblyNames(context))
    //                Console.WriteLine($"Runtime {runtimeLibrary.Name}:{assembly.Name}");
    //        }
    //    }
    //}
}
