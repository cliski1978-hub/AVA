using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AVA.UPS.Adapter.Models;

namespace AVA.UPS.Adapter.Validation
{
    /// <summary>
    /// Provides UPS-wide semantic type validation.
    /// Fully supports primitives, lists, identity,
    /// embeddings, domain-specific types, and a pluggable type registry.
    /// </summary>
    public static class UParamTypeChecker
    {
        // --------------------------------------------------------------------
        // 1. Built-in primitive + domain semantic types
        // --------------------------------------------------------------------
        private static readonly HashSet<string> PrimitiveTypes = new()
        {
            "string", "int", "float", "bool", "datetime"
        };

        private static readonly HashSet<string> DomainTypes = new()
        {
            // Informational domain types used across AVA
            "identity",
            "embedding",
            "memoryRecord",
            "uParamBlock"
        };

        // --------------------------------------------------------------------
        // 2. Pluggable semantic type registry (for module extensions)
        // --------------------------------------------------------------------
        private static readonly Dictionary<string, Func<object, bool>> CustomCheckers = new();

        /// <summary>
        /// Registers a custom semantic type checker.
        /// Module adaptors may extend UPS types dynamically.
        /// </summary>
        public static void RegisterType(string type, Func<object, bool> checker)
        {
            CustomCheckers[type] = checker;
        }

        /// <summary>
        /// Removes a registered semantic type checker.
        /// </summary>
        public static void UnregisterType(string type)
        {
            if (CustomCheckers.ContainsKey(type))
                CustomCheckers.Remove(type);
        }

        // --------------------------------------------------------------------
        // 3. Main semantic validation entry point
        // --------------------------------------------------------------------
        public static bool IsTypeValid(UParam param, string expectedType)
        {
            if (param.Value == null)
                return false;

            // List<T> semantic type?
            if (expectedType.StartsWith("list<") && expectedType.EndsWith(">"))
                return ValidateList(param.Value, expectedType);

            // Registered custom type?
            if (CustomCheckers.TryGetValue(expectedType, out var checker))
                return checker(param.Value);

            // Built-in primitive?
            if (PrimitiveTypes.Contains(expectedType))
                return ValidatePrimitive(param.Value, expectedType);

            // Built-in domain?
            if (DomainTypes.Contains(expectedType))
                return ValidateDomain(param.Value, expectedType);

            // If unknown semantic type -> invalid (UPS does NOT allow silent passing)
            return false;
        }

        // --------------------------------------------------------------------
        // 4. Primitive Validators
        // --------------------------------------------------------------------
        private static bool ValidatePrimitive(object value, string type)
        {
            return type switch
            {
                "string" => value is string,
                "int" => value is int || value is long || TryCoerceToInt(value),
                "float" => value is float || value is double || value is decimal || TryCoerceToFloat(value),
                "bool" => value is bool || TryCoerceToBool(value),
                "datetime" => value is DateTime || TryCoerceToDateTime(value),
                _ => false
            };
        }

        private static bool TryCoerceToInt(object value)
        {
            try
            {
                _ = Convert.ToInt64(value);
                return true;
            }
            catch { return false; }
        }

        private static bool TryCoerceToFloat(object value)
        {
            try
            {
                _ = Convert.ToDouble(value);
                return true;
            }
            catch { return false; }
        }

        private static bool TryCoerceToBool(object value)
        {
            return bool.TryParse(value.ToString(), out _);
        }

        private static bool TryCoerceToDateTime(object value)
        {
            return DateTime.TryParse(value.ToString(), out _);
        }

        // --------------------------------------------------------------------
        // 5. Domain Validators (Identity, Embeddings, etc.)
        // --------------------------------------------------------------------
        private static bool ValidateDomain(object value, string type)
        {
            return type switch
            {
                "identity" => ValidateIdentity(value),
                "embedding" => ValidateEmbedding(value),
                "memoryRecord" => ValidateMemoryRecord(value),
                "uParamBlock" => ValidateUParamBlock(value),
                _ => false
            };
        }

        private static bool ValidateIdentity(object value)
        {
            // Identity can be:
            // - Primary string id/handle
            // - Dictionary<string, object> identity packet
            // - Structured object containing identity keys

            if (value is string s && !string.IsNullOrWhiteSpace(s))
                return true;

            if (value is Dictionary<string, object> dict)
            {
                return dict.ContainsKey("id") ||
                       dict.ContainsKey("handle") ||
                       dict.ContainsKey("type");
            }

            return false;
        }

        private static bool ValidateEmbedding(object value)
        {
            if (value is float[] fa)
                return ValidateEmbeddingArray(fa);

            if (value is List<float> fl)
                return ValidateEmbeddingArray(fl.ToArray());

            if (value is IEnumerable list)
            {
                try
                {
                    var floats = list.Cast<object>().Select(Convert.ToSingle).ToArray();
                    return ValidateEmbeddingArray(floats);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static bool ValidateEmbeddingArray(float[] arr)
        {
            if (arr.Length == 0) return false;

            foreach (var f in arr)
                if (float.IsNaN(f) || float.IsInfinity(f))
                    return false;

            // Could enforce embedding length constraints here if desired.
            return true;
        }

        private static bool ValidateMemoryRecord(object value)
        {
            // MemoryRecord must have content + metadata
            if (value is Dictionary<string, object> dict)
            {
                return dict.ContainsKey("id") &&
                       dict.ContainsKey("text");
            }

            return false;
        }

        private static bool ValidateUParamBlock(object value)
        {
            // A UParamBlock is a list/dictionary of UParams
            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                    if (item is not UParam) return false;

                return true;
            }

            return false;
        }

        // --------------------------------------------------------------------
        // 6. Generic List<T> Validation
        // --------------------------------------------------------------------
        private static bool ValidateList(object value, string semanticListType)
        {
            if (value is not IEnumerable enumerable)
                return false;

            string innerType = semanticListType[5..^1]; // extract T from list<T>

            foreach (var item in enumerable)
            {
                if (item == null) return false;

                // Wrap each element as UParam for type reuse
                var tempParam = new UParam
                {
                    Key = "listElement",
                    Type = innerType,
                    Value = item
                };

                if (!IsTypeValid(tempParam, innerType))
                    return false;
            }

            return true;
        }
    }
}
