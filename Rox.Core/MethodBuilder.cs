using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Rox.Core
{
    public static class MethodProxyToServiceProvider
    {
        public static object[] BuildParameters(this MethodInfo MethodInfo, IServiceProvider serviceProvider, params dynamic[] sourceParameters)
        {
            var parameterInfos = MethodInfo.GetParameters();
            var parameters = new object[parameterInfos.Length];
            var sameParameters = new Dictionary<string, int>();
            for (var index = 0; index < parameterInfos.Length; index++)
            {
                object parameterResult = null;
                var parameterInfo = parameterInfos[index];
                var fullNameType = parameterInfo.ParameterType.FullName;
                var suitables = sourceParameters.Where(p => p.GetType().FullName == fullNameType).ToList();

                if (suitables.Count > 1)
                {
                    if (sameParameters.TryGetValue(fullNameType, out int indexSame))
                    {
                        sameParameters[fullNameType]++;
                    }

                    if (indexSame < suitables.Count)
                    {
                        parameterResult = suitables[indexSame];
                    }
                }
                else if (suitables.Count == 1)
                    parameterResult = suitables.First();

                if (parameterResult == null)
                {
                    if (parameterInfo.ParameterType == typeof(IServiceProvider))
                    {
                        parameterResult = serviceProvider;
                    }
                    else
                    {
                        try
                        {
                            parameterResult = serviceProvider.GetRequiredService(parameterInfo.ParameterType);
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

                if (parameterResult != null)
                    parameters[index] = parameterResult;
            }

            return parameters;
        }
    }

    public class MethodProxy
    {
        public MethodProxy(MethodInfo configure)
        {
            this.MethodInfo = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        public MethodInfo MethodInfo { get; }

        public FuncParams<object> Build(object instance, IServiceProvider servicePrivider) => tocken => Invoke(instance, servicePrivider, tocken);

        private object Invoke(object instance, IServiceProvider serviceProvider, params object[] tocken)
        {
            var parameterInfos = MethodInfo.GetParameters();
            var parameters = new object[parameterInfos.Length];
            var sameParameters = new Dictionary<string, int>();
            for(var index = 0; index < parameterInfos.Length; index++)
            {
                object parameterResult = null;
                var parameterInfo = parameterInfos[index];
                var fullNameType = parameterInfo.ParameterType.FullName;
                var suitables = tocken.Where(p => p.GetType().FullName == fullNameType).ToList();

                if (suitables.Count > 1)
                {
                    if (sameParameters.TryGetValue(fullNameType, out int indexSame))
                    {
                        sameParameters[fullNameType]++;
                    }

                    if (indexSame < suitables.Count)
                    {
                        parameterResult = suitables[indexSame];
                    }
                }
                else if (suitables.Count == 1)
                    parameterResult = suitables.First();

                if (parameterResult == null)
                {
                    if (parameterInfo.ParameterType == typeof(IServiceProvider))
                    {
                        parameterResult = serviceProvider;
                    }
                    else
                    {
                        try
                        {
                            parameterResult = serviceProvider.GetRequiredService(parameterInfo.ParameterType);
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

                if (parameterResult != null)
                    parameters[index] = parameterResult;
            }

            return MethodInfo.Invoke(instance, parameters);
            //return result as Task ?? Task.FromResult(result);
        }
    }
}