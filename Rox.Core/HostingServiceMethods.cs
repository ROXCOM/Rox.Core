using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Rox.Core
{
    public class HostingServiceMethods : LinkedList<FuncParams<object>>
    {
        public HostingServiceMethods(object instance, params FuncParams<object>[] delegates)
        {
            Instance = instance;
            Delegates = new LinkedList<FuncParams<object>>(delegates);
        }

        private object Instance { get; }
        public LinkedList<FuncParams<object>> Delegates { get; }
    }
}