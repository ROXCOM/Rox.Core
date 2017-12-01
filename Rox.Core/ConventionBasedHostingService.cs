using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Hosting;

namespace Rox.Core
{
    public class ConventionBasedHostingService : IHostedService
    {
        private readonly HostingServiceMethods methods;

        public ConventionBasedHostingService(HostingServiceMethods methods) {
            this.methods = methods;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try {
                return methods.StartDelegate(cancellationToken);
            }
            catch (Exception ex) {
                if (ex is TargetInvocationException)
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try {
                return methods.StopDelegate(cancellationToken);
            }
            catch (Exception ex) {
                if (ex is TargetInvocationException)
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                throw;
            }
        }
    }
}