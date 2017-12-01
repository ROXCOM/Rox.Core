using System;
using System.IO;
using System.Linq;
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
    internal class IndividualAssemblyLoadContext : AssemblyLoadContext
    {
        //internal IndividualAssemblyLoadContext() : base(false)
        //{
        //}

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
