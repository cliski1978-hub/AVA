using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Contracts;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Validation;

namespace AVA.UPS.Adapter
{
    /// <summary>
    /// A fully semantic, contract-aware, async-capable UPS ↔ DTO mapper.
    /// This class serves as the foundation for all UPS DTO adapters and is
    /// designed for UPS v1.0+ and AVA's future agent-driven data flows.
    /// </summary>
    public abstract class BaseUParamAdapter : IUParamAdapter
    {
        // --------------------------------------------------------------------
        // Configurable Behavior Flags
        // --------------------------------------------------------------------

        public bool StrictMode { get; set; } = false;
        public bool AutoMapIdentity { get; set; } = true;
        public bool AutoNormalizeEmbeddings { get; set; } = true;
        public bool EnableNestedDTOs { get; set; } = true;

        // --------------------------------------------------------------------
        // Public Synchronous API
        // --------------------------------------------------------------------

        public virtual T Parse<T>(List<UParam> parameters) where T : new()
            => ParseInternal<T>(parameters, contract: null);

        public virtual List<UParam> Build<T>(T dto)
            => BuildInternal(dto, contract: null);

        public virtual T Parse<T>(List<UParam> parameters, UPSMethodContract contract)
            where T : new()
            => ParseInternal<T>(parameters, contract);

        public virtual List<UParam> Build<T>(T dto, UPSMethodContract contract)
            => BuildInternal(dto, contract);

        // --------------------------------------------------------------------
        // Public Asynchronous API (future remote + identity + vector DB support)
        // --------------------------------------------------------------------

        public virtual Task<T> ParseAsync<T>(List<UParam> parameters) where T : new()
            => Task.FromResult(ParseInternal<T>(parameters, null));

        public virtual Task<List<UParam>> BuildAsync<T>(T dto)
            => Task.FromResult(BuildInternal(dto, null));

        public virtual Task<T> ParseAsync<T>(List<UParam> parameters, UPSMethodContract contract)
            where T : new()
            => Task.FromResult(ParseInternal<T>(parameters, contract));

        public virtual Task<List<UParam>> BuildAsync<T>(T dto, UPSMethodContract contract)
            => Task.FromResult(BuildInternal(dto, contract));

        // --------------------------------------------------------------------
        // Core Parsing Logic (UPS → DTO)
        // --------------------------------------------------------------------

        protected virtual T ParseInternal<T>(List<UParam> parameters, UPSMethodContract? contract) where T : new()
        {
            T dto = new();
            Type type = typeof(T);

            foreach (var prop in GetMappableProperties(type))
            {
                string key = GetMappedKey(prop);

                // Find matching UParam
                var param = parameters.FirstOrDefault(p =>
                    p.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

                if (param == null)
                {
                    if (StrictMode)
                        throw new Exception($"Missing required DTO field '{key}' in StrictMode.");
                    continue;
                }

                object? value = ConvertToPropertyType(prop, param.Value, param.Type, contract);

                prop.SetValue(dto, value);
            }

            return dto;
        }

        // --------------------------------------------------------------------
        // Core Build Logic (DTO → UPS)
        // --------------------------------------------------------------------

        protected virtual List<UParam> BuildInternal<T>(T dto, UPSMethodContract? contract)
        {
            var list = new List<UParam>();
            Type type = typeof(T);

            foreach (var prop in GetMappableProperties(type))
            {
                object? value = prop.GetValue(dto);
                if (value == null)
                {
                    if (StrictMode)
                        throw new Exception($"Null field '{prop.Name}' is not allowed in StrictMode.");
                    continue;
                }

                var uparam = BuildSingleUParam(prop, value, contract);
                list.Add(uparam);
            }

            return list;
        }

        // --------------------------------------------------------------------
        // Property Reflection Helpers
        // --------------------------------------------------------------------

        protected IEnumerable<PropertyInfo> GetMappableProperties(Type type)
            => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   .Where(p => p.CanRead && p.CanWrite);

        protected string GetMappedKey(PropertyInfo prop)
            => prop.Name.Trim(); // (Future: support custom attributes)

        // --------------------------------------------------------------------
        // UPS → DTO Type Conversion (domain-aware)
        // --------------------------------------------------------------------

        protected object? ConvertToPropertyType(
            PropertyInfo prop,
            object rawValue,
            string? semanticType,
            UPSMethodContract? contract)
        {
            Type expectedType = prop.PropertyType;

            // If types already match, return directly
            if (expectedType.IsAssignableFrom(rawValue.GetType()))
                return rawValue;

            // ---------------------------
            // Primitive Coercion
            // ---------------------------
            try
            {
                if (expectedType == typeof(string)) return rawValue.ToString();
                if (expectedType == typeof(int)) return Convert.ToInt32(rawValue);
                if (expectedType == typeof(long)) return Convert.ToInt64(rawValue);
                if (expectedType == typeof(float)) return Convert.ToSingle(rawValue);
                if (expectedType == typeof(double)) return Convert.ToDouble(rawValue);
                if (expectedType == typeof(bool)) return Convert.ToBoolean(rawValue);
            }
            catch
            {
                if (StrictMode)
                    throw new Exception($"Cannot convert value '{rawValue}' to '{expectedType.Name}'.");
            }

            // ---------------------------
            // Domain Type Decoding
            // ---------------------------

            // Embedding → float[]
            if (expectedType == typeof(float[]) &&
                semanticType == "embedding")
            {
                return ConvertEmbedding(rawValue);
            }

            // Identity → string or packet mapping
            if (expectedType == typeof(string) &&
                semanticType == "identity")
            {
                return ConvertIdentity(rawValue);
            }

            // Structured DTO
            if (EnableNestedDTOs && IsDTO(expectedType) && rawValue is Dictionary<string, object> dict)
            {
                return ConvertNestedDTO(expectedType, dict);
            }

            // List<T> DTOs or primitives
            if (expectedType.IsGenericType &&
                typeof(IEnumerable).IsAssignableFrom(expectedType))
            {
                return ConvertList(expectedType, rawValue);
            }

            // Last resort
            if (!StrictMode)
                return rawValue;

            throw new Exception($"Unsupported UPS → DTO conversion: raw='{rawValue}', expectedType='{expectedType}'.");
        }

        // --------------------------------------------------------------------
        // DTO → UPS Type Conversion (domain-aware)
        // --------------------------------------------------------------------

        protected UParam BuildSingleUParam(PropertyInfo prop, object value, UPSMethodContract? contract)
        {
            string key = GetMappedKey(prop);
            Type propType = prop.PropertyType;

            string semanticType = MapSemanticType(propType);

            object outputValue = value;

            // Domain type encoders:
            if (semanticType == "embedding" && AutoNormalizeEmbeddings)
                outputValue = NormalizeEmbedding(value);

            if (EnableNestedDTOs && IsDTO(propType) && semanticType == "uParamBlock")
                outputValue = ConvertNestedDTOToBlock(value);

            return new UParam
            {
                Key = key,
                Type = semanticType,
                Value = outputValue
            };
        }

        // --------------------------------------------------------------------
        // Embedding Helpers
        // --------------------------------------------------------------------

        protected float[] ConvertEmbedding(object raw)
        {
            if (raw is float[] fa) return fa;
            if (raw is IEnumerable enumerable)
            {
                var floats = enumerable.Cast<object>()
                                       .Select(Convert.ToSingle)
                                       .ToArray();
                return floats;
            }
            throw new Exception("Invalid embedding format.");
        }

        protected float[] NormalizeEmbedding(object value)
        {
            if (value is float[] fa) return fa;
            if (value is IEnumerable enumerable)
            {
                return enumerable.Cast<object>()
                                 .Select(Convert.ToSingle)
                                 .ToArray();
            }
            throw new Exception("Invalid embedding source for normalization.");
        }

        // --------------------------------------------------------------------
        // Identity Helpers
        // --------------------------------------------------------------------

        protected object ConvertIdentity(object raw)
        {
            if (raw is string s) return s;
            if (raw is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("id", out var id))
                    return id.ToString()!;
                if (dict.TryGetValue("handle", out var h))
                    return h.ToString()!;
            }
            throw new Exception("Invalid identity format.");
        }

        // --------------------------------------------------------------------
        // Nested DTO Helpers
        // --------------------------------------------------------------------

        protected bool IsDTO(Type t)
            => t.IsClass && t != typeof(string) && !t.IsPrimitive;

        protected object ConvertNestedDTO(Type expectedType, Dictionary<string, object> dict)
        {
            object dto = Activator.CreateInstance(expectedType)!;

            foreach (var prop in GetMappableProperties(expectedType))
            {
                string key = GetMappedKey(prop);
                if (!dict.TryGetValue(key, out var raw))
                    continue;

                object? value = ConvertToPropertyType(prop, raw, semanticType: null, contract: null);
                prop.SetValue(dto, value);
            }

            return dto;
        }

        protected Dictionary<string, object> ConvertNestedDTOToBlock(object dto)
        {
            var dict = new Dictionary<string, object>();
            Type t = dto.GetType();

            foreach (var prop in GetMappableProperties(t))
            {
                object? value = prop.GetValue(dto);
                if (value != null)
                    dict[prop.Name] = value;
            }

            return dict;
        }

        // --------------------------------------------------------------------
        // List<T> Helpers
        // --------------------------------------------------------------------

        protected object ConvertList(Type expectedType, object raw)
        {
            if (raw is not IEnumerable enumerable)
                throw new Exception($"Value is not a list: {raw}");

            Type innerType = expectedType.GetGenericArguments()[0];

            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(innerType))!;

            foreach (var item in enumerable)
            {
                object? v = ConvertToListElement(innerType, item);
                list.Add(v!);
            }

            return list;
        }

        protected object? ConvertToListElement(Type innerType, object raw)
        {
            if (IsDTO(innerType))
                return ConvertNestedDTO(innerType, raw as Dictionary<string, object> ?? new());

            try
            {
                if (innerType == typeof(int)) return Convert.ToInt32(raw);
                if (innerType == typeof(long)) return Convert.ToInt64(raw);
                if (innerType == typeof(float)) return Convert.ToSingle(raw);
                if (innerType == typeof(double)) return Convert.ToDouble(raw);
                if (innerType == typeof(bool)) return Convert.ToBoolean(raw);
                if (innerType == typeof(string)) return raw.ToString();
            }
            catch
            {
                if (StrictMode)
                    throw;
            }

            return raw;
        }

        // --------------------------------------------------------------------
        // Semantic Type Mapping
        // --------------------------------------------------------------------

        protected virtual string MapSemanticType(Type t)
        {
            if (t == typeof(string)) return "string";
            if (t == typeof(int) || t == typeof(long)) return "int";
            if (t == typeof(float) || t == typeof(double)) return "float";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(DateTime)) return "datetime";
            if (t == typeof(float[])) return "embedding";

            if (typeof(IEnumerable<string>).IsAssignableFrom(t)) return "list<string>";
            if (typeof(IEnumerable<int>).IsAssignableFrom(t)) return "list<int>";
            if (typeof(IEnumerable<float>).IsAssignableFrom(t)) return "list<float>";
            if (typeof(IEnumerable<double>).IsAssignableFrom(t)) return "list<float>";

            if (EnableNestedDTOs && IsDTO(t)) return "uParamBlock";

            return t.Name; // semantic/custom fallback
        }
    }
}
