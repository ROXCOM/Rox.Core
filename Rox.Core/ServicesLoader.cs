using System;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Rox.Core
{
    public static class ServicesLoader
    {
        /// <summary>
        /// Производит поиск метода в переданном типе
        /// </summary>
        /// <param name="type">Тип в котором производиться поиск метода methodName</param>
        /// <param name="methodName">Имя метода</param>
        /// <param name="environmentName">Используется для поиска названия метода с учетом переменной среды. К примеру если передан methodName: Start{0}Async, а environmentName: Development. то в таком случае поииску подлежат 2 метода: StartDevelopmentAsync (в первую очередь) и StartAsync во вторую очередь</param>
        /// <param name="returnType">Если указано, то производиться поиск метода, который имеет возвращаемый тип с указанного типа. Если не указано, то будет найден метод, с любым возвращаемым типом</param>
        /// <returns></returns>
        private static IList<MethodInfo> FindMethods(this Type type, string methodName, string environmentName = "", Type returnType = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException(nameof(methodName));

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            var methodNameWithEnv = string.Format(CultureInfo.InvariantCulture, methodName, environmentName);
            var methodNameWithNoEnv = string.Format(CultureInfo.InvariantCulture, methodName, "");

            // Произвожу поиск методов с именем methodNameWithEnv
            // Если таковых нет, то с именем methodNameWithNoEnv
            var selectedMethods = methods
                .AsParallel()
                .Where(method => method.Name.Equals(methodNameWithEnv))
                .Where(method => (returnType == null) || (returnType != null && method.ReturnType.FullName.Equals(returnType.FullName, StringComparison.InvariantCulture)))
                .ToList();

            if (selectedMethods.Count == 0)
            {
                selectedMethods = methods
                    .AsParallel()
                    .Where(method => method.Name.Equals(methodNameWithNoEnv))
                    .Where(method => (returnType == null) || (returnType != null && method.ReturnType.FullName.Equals(returnType.FullName, StringComparison.InvariantCulture)))
                    .ToList();
            }
            if (selectedMethods.Count == 0)
            {
                return null;
            }

            return selectedMethods;
        }

        /// <summary>
        /// Производит поиск метода Start
        /// </summary>
        /// <param name="type"></param>
        /// <param name="environmentName"></param>
        /// <returns>MethodInfo если найден подходящий метод. null, если подходящий метод не найден</returns>
        private static IEnumerable<MethodInfo> FindStartMethods(this Type type, string environmentName = "")
        {
            // Если не найдены методы текущего FindMethods, то проверяется следующий
            return
                FindMethods(type, "Start{0}Async", environmentName, returnType: typeof(Task)) ??
                FindMethods(type, "Start{0}", environmentName, returnType: typeof(Task)) ??
                FindMethods(type, "Start{0}Async", environmentName, returnType: typeof(void)) ??
                FindMethods(type, "Start{0}", environmentName, returnType: typeof(void));
        }

        /// <summary>
        /// Производит поиск метода Stop
        /// </summary>
        /// <param name="startType"></param>
        /// <param name="environmentName"></param>
        /// <returns>MethodInfo если найден подходящий метод. null, если подходящий метод не найден</returns>
        private static IEnumerable<MethodInfo> FindStopMethods(this Type type, string environmentName = "")
        {
            // Если не найдены методы текущего FindMethods, то проверяется следующий
            return
                FindMethods(type, "Stop{0}Async", environmentName, returnType: typeof(Task)) ??
                FindMethods(type, "Stop{0}", environmentName, returnType: typeof(Task)) ??
                FindMethods(type, "Stop{0}Async", environmentName, returnType: typeof(void)) ??
                FindMethods(type, "Stop{0}", environmentName, returnType: typeof(void));
        }

        /// <summary>
        /// Поиск методов в типе, подходящих под сигнатуру IHostedService методов
        /// environmentName работает так: если в типе определено 2 метода StartAsync, Start{environmentName}Async, то приоритетно будет выбран второй
        /// </summary>
        /// <param name="type">Тип в котором производиться поиск методов</param>
        /// <param name="environmentName">В приоритет выбираются имена методов у которых в названии существует environmentName</param>
        /// <returns></returns>
        public static (MethodInfo, MethodInfo) SearchHostedServiceCandidateMethods(this Type type, string environmentName = "")
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return
            (
                type.FindStartMethods(environmentName)?.FirstOrDefault(),
                type.FindStopMethods(environmentName)?.FirstOrDefault()
            );
        }

        public static bool IsCandidateMethods(this (MethodInfo, MethodInfo) p) =>
            p.Item1 != null &&
            p.Item2 != null;

        public static bool IsCandidateType(this Type p) =>
            p.IsPublic &&
            p.IsClass &&
            !p.IsAbstract;
        
        /// <summary>
        /// Поиск типов в сборке, подходящих под сигнатуру IHostedService
        /// environmentName работает так: если в типе определено 2 метода StartAsync, Start{environmentName}Async, то приоритетно будет выбран второй
        /// </summary>
        /// <param name="assembly">Сборка в которой производиться поиск</param>
        /// <param name="environmentName">В приоритет выбираются имена методов у которых в названии существует environmentName</param>
        /// <returns></returns>
        public static IEnumerable<Type> SearchCandidateTypes(this Assembly assembly, string environmentName = "")
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            // произвожу полное сканирование типов, определенных в сборке assembly
            return assembly.DefinedTypes
                //.AsParallel()
                .Where(info => info.IsCandidateType())
                .ToList();
        }
    }
}
