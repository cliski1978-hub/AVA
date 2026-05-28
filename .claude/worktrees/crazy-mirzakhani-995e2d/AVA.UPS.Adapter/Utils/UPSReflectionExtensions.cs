using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AVA.UPS.Adapter.Models;

namespace AVA.UPS.Adapter.Utils
{
    /// <summary>
    /// Reflection helpers for discovering UPS-compatible methods.
    /// </summary>
    public static class UPSReflectionExtensions
    {
        /// <summary>
        /// Returns all methods that match the UPS method signature:
        ///     Task<List<UParam>> Method(List<UParam> parameters)
        /// </summary>
        public static IEnumerable<MethodInfo> GetUPSMethods(this Type type)
        {
            return type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(IsUPSMethod);
        }

        public static bool IsUPSMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();

            var correctParams =
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(List<UParam>);

            var correctReturn =
                method.ReturnType == typeof(Task<List<UParam>>);

            return correctParams && correctReturn;
        }

        /// <summary>
        /// Gets a UPS method by name (case-insensitive).
        /// </summary>
        public static MethodInfo? GetUPSMethod(this Type type, string methodName)
        {
            return type
                .GetUPSMethods()
                .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
