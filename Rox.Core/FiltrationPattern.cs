using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;

namespace Rox.Core
{
    public static class FiltrationPattern
    {
        public static bool IsDLL(this IFileInfo info, string inDirectory = "")
        {
            return info.Exists && (info.IsDirectory || (info.PhysicalPath.Contains(inDirectory) && info.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)));
        }

        public static bool RegexPattern(this IFileInfo info, Regex regex)
        {
            return info.Exists && (info.IsDirectory || regex.IsMatch(info.PhysicalPath));
        }

        public static bool RegexPattern(this IFileInfo info, string pattern)
        {
            var regex = new Regex(pattern);
            return info.Exists && (info.IsDirectory || regex.IsMatch(info.PhysicalPath));
        }

        public static bool RegexPattern(this Assembly assembly, Regex regex)
        {
            return regex.IsMatch(assembly.FullName);
        }

        public static bool RegexPattern(this Assembly assembly, string pattern)
        {
            var regex = new Regex(pattern);
            return regex.IsMatch(assembly.FullName);
        }

        /// <summary>
        /// Паттерн для исключения системных библиотек
        /// Необходимо для того, что бы не сканировать попросту их и не затрачивать время
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        internal static bool IsSystemLibrary(this Assembly assembly)
        {
            return
                assembly.ManifestModule.Name.StartsWith("Rox.Core", StringComparison.OrdinalIgnoreCase) ||
                assembly.ManifestModule.Name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                assembly.ManifestModule.Name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                assembly.ManifestModule.Name.StartsWith("NETStandard.Library", StringComparison.OrdinalIgnoreCase) ||
                assembly.ManifestModule.Name.StartsWith("Libuv", StringComparison.OrdinalIgnoreCase);
        }
    }
}