using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Rox.Core
{
    public class MethodBuilder
    {
        public MethodBuilder(MethodInfo configure)
        {
            this.MethodInfo = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        public MethodInfo MethodInfo { get; }

        public Func<CancellationToken, Task> Build(object instance, IServiceProvider servicePrivider) => tocken => Invoke(instance, servicePrivider, tocken);

        private Task Invoke(object instance, IServiceProvider serviceProvider, CancellationToken tocken)
        {
            var parameterInfos = MethodInfo.GetParameters();
            var parameters = new object[parameterInfos.Length];
            for(var index = 0; index < parameterInfos.Length; index++)
            {
                var parameterInfo = parameterInfos[index];
                if (parameterInfo.ParameterType.FullName == typeof(CancellationToken).FullName)
                {
                    parameters[index] = tocken;
                }
                else if (parameterInfo.ParameterType == typeof(IServiceProvider))
                {
                    parameters[index] = serviceProvider;
                }
                else
                {
                    try
                    {
                        parameters[index] = serviceProvider.GetRequiredService(parameterInfo.ParameterType);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format(
                            "Could not resolve a service of type '{0}' for the parameter '{1}' of method '{2}' on type '{3}'.",
                            parameterInfo.ParameterType.FullName,
                            parameterInfo.Name,
                            MethodInfo.Name,
                            MethodInfo.DeclaringType.FullName), ex);
                    }
                }
            }

            var result = MethodInfo.Invoke(instance, parameters);
            return result as Task ?? Task.FromResult(result);
        }
    }
}