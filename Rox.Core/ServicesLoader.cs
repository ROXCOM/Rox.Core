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
        public static IEnumerable<MethodInfo> FindMethods(this Type type, string methodName, string environmentName = "", Type returnType = null)
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
                return null;

            return selectedMethods;
        }

        /// <summary>
        /// Производит поиск методов с указанным именем methodName
        /// Если указан environmentName, то в первую очередь птается найти метод с именем "methodName{environmentName}", потом "methodName"
        /// Если не найден метод с именем methodName, то производиться поиск асинхронного метода с именем "methodNameAsync"
        /// Если тип возвращаемого значения не указан, то производиться поиск метода с возвращаемым типом "Task" и далее "Void"
        /// </summary>
        /// <param name="type">Тип в котором производиться поиск методов</param>
        /// <param name="environmentName">Опционально. Если указано значение, то в приоритете производиться поиск метода, в котором присутствует в названии указанная строка в следующем виде: "methodName + {environmentName}"</param>
        /// <returns>MethodInfo если найден подходящий метод. null, если подходящий метод не найден</returns>
        private static IEnumerable<MethodInfo> FindIncludingAsyncMethods(this Type type, string methodName, string environmentName = "", Type returnType = null)
        {
            // Если не найдены методы текущего FindMethods, то проверяется следующий
            return
                FindMethods(type, methodName + "{0}Async", environmentName, returnType: returnType ?? typeof(Task)) ??
                FindMethods(type, methodName + "{0}", environmentName, returnType: returnType ?? typeof(Task)) ??
                FindMethods(type, methodName + "{0}Async", environmentName, returnType ?? typeof(void)) ??
                FindMethods(type, methodName + "{0}", environmentName, returnType ?? typeof(void));
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

        public static IDictionary<MethodInfo, IEnumerable<MethodInfo>> FindMethods(this Type inType, Type protoType, string environmentName = "")
        {
            if (inType == null)
                throw new ArgumentNullException(nameof(inType));
            if (protoType == null)
                throw new ArgumentNullException(nameof(protoType));

            var result = new Dictionary<MethodInfo, IEnumerable<MethodInfo>>();
            var methods = protoType.GetTypeInfo().DeclaredMethods.Where(p => p.IsPublic);
            foreach(var methodForSearch in methods)
            {
                var returnType = methodForSearch.ReturnType;
                if (methodForSearch.ReturnType == typeof(void) || methodForSearch.ReturnType == typeof(Task))
                    returnType = null;

                var name = methodForSearch.Name;
                if (name.EndsWith("Async", StringComparison.OrdinalIgnoreCase) && !name.Equals("Async", StringComparison.OrdinalIgnoreCase))
                    name = name.Split("Async").First();

                result.Add(methodForSearch, inType.FindIncludingAsyncMethods(name, environmentName, returnType));
            }

            return result;
        }

        public static string Info(this MethodInfo info)
        {
            return $"{info.ReturnType.FullName} {info.Name}({string.Join(", ", info.GetParameters().OrderBy(p => p.Position).Select(p => $"{p.ParameterType} {p.Name}"))})";
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
