using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Rox.Core
{
    public class ConventionBasedType
    {
        private IList<DynamicMethod> dinamicMethods = new List<DynamicMethod>(); 

        public ConventionBasedType(HostingServiceMethods methods)
        { }

        public TPrototype Create<TPrototype>()
        {
            var type = typeof(TPrototype);
            if (type.IsInterface)
            {
                var dynamicMethods = new List<DynamicMethod>();
                var methods = type.GetMethods(BindingFlags.Public);
                foreach(var method in methods)
                {
                    var generator = new DynamicMethod(
                        method.Name,
                        method.ReturnType,
                        method.GetParameters().Select(p => p.ParameterType).ToArray(),
                        this.GetType().Module
                    );
                    generator.GetILGenerator();
                }
            }
            else
            {

            }
            throw new NotImplementedException();
        }
    }

    public class ConventionBased
    {
        protected Type type;
        protected object instance;
        private IDictionary<MethodInfo, IEnumerable<MethodInfo>> methodInfos;
        private ILogger logger;

        public ConventionBased(Type type, IServiceProvider provider)
        {
            this.type = type;
            this.Provider = provider;
            //this.logger = provider.GetService<ILoggerFactory>().CreateLogger(type);

            Initialization();
        }

        public IServiceProvider Provider { get; protected set; }
        public IEnumerable<MethodInfo> Methods { get { return this.GetType().GetTypeInfo().DeclaredMethods; } }

        private void Initialization()
        {
            this.methodInfos = this.GetType().FindMethods(protoType: type);

            //this.instance = ActivatorUtilities.GetServiceOrCreateInstance(Provider, type);
            this.instance = Activator.CreateInstance(type);
        }

        private (MethodInfo, MethodInfo) MethodInfo(string name)
        {
            var method = this.GetType().GetMethod(name);
            var info = methodInfos[method]?.FirstOrDefault();
            if (info == null)
                throw new Exception($"Class '{type.FullName}' does not contain a method '{method.Info()}'");

            return (method, info);
        }

        protected FuncParams<k, t> DelegateFunc<k, t>(string name)
        {
            var methods = MethodInfo(name);

            return (o, parameters) =>
            {
                try
                {
                    return (t)methods.Item2.Invoke(o, methods.Item1.BuildParameters(Provider, parameters));
                }
                catch (Exception e)
                {
                    if (e is TargetInvocationException)
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();

                    throw;
                }
            };
        }

        protected ActionParams<k> DelegateAction<k>(string name)
        {
            var methods = MethodInfo(name);

            return (o, parameters) =>
            {
                try
                {
                    methods.Item2.Invoke(o, methods.Item1.BuildParameters(Provider, parameters));
                }
                catch (Exception e)
                {
                    if (e is TargetInvocationException)
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();

                    throw;
                }
            };
        }
    }

    public class ConventionBasedHostedService : ConventionBased, IHostedService
    {
        public ConventionBasedHostedService(Type type, IServiceProvider provider) : base(type, provider)
        { }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return DelegateFunc<object, Task>("StartAsync")(this.instance, cancellationToken);
        }

        public Task Start(CancellationToken cancellationToken, string name, int i = 0, double qwe = 0.00)
        {
            var action = DelegateFunc<object, Task>("StartAsync");
            return action(this.instance, cancellationToken, name, i, qwe);
        }

        public void Stop(CancellationToken cancellationToken, string name, int i = 0, double qwe = 0.00)
        {
            var action = DelegateAction<object>("Stop");
            action(this.instance, cancellationToken, name, i, qwe);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}