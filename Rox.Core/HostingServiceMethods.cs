using System;
using System.Threading.Tasks;
using System.Threading;

namespace Rox.Core
{
    public class HostingServiceMethods
    {
        public HostingServiceMethods(object instance, Func<CancellationToken, Task> start, Func<CancellationToken, Task> stop)
        {
            Instance = instance;
            StartDelegate = start;
            StopDelegate  = stop;
        }

        private object Instance { get; }
        public Func<CancellationToken, Task> StartDelegate { get; }
        public Func<CancellationToken, Task> StopDelegate { get; }
    }
}
