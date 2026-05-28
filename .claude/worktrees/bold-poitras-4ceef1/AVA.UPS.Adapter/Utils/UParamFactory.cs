using System;
using System.Collections.Generic;
using System.Text.Json;
using AVA.UPS.Adapter.Models;

namespace AVA.UPS.Adapter.Utils
{
    /// <summary>
    /// Factory helpers for building strongly typed UParams.
    /// Fully DUAS-compliant and compatible with ContractValidator + BaseUParamAdapter.
    /// </summary>
    public static class UParamFactory
    {
        // ---------------------------------------------------------------------
        //  GENERIC FACTORY
        // ---------------------------------------------------------------------
        public static UParam Create(string key, string type, object? value)
        {
            return new UParam
            {
                Key = key,
                Type = type,
                Value = value
            };
        }

        // ---------------------------------------------------------------------
        //  PRIMITIVE TYPES
        // ---------------------------------------------------------------------
        public static UParam String(string key, string? value) =>
            Create(key, "string", value);

        public static UParam Int(string key, int value) =>
            Create(key, "int", value);

        public static UParam Long(string key, long value) =>
            Create(key, "int", value);

        public static UParam Bool(string key, bool value) =>
            Create(key, "bool", value);

        public static UParam Float(string key, float value) =>
            Create(key, "float", value);

        public static UParam Double(string key, double value) =>
            Create(key, "float", value);

        // ---------------------------------------------------------------------
        //  EMBEDDINGS
        // ---------------------------------------------------------------------
        public static UParam Embedding(string key, float[] vector) =>
            Create(key, "embedding", vector);

        public static UParam EmbeddingList(string key, List<float> vector) =>
            Create(key, "embedding", vector);

        // ---------------------------------------------------------------------
        //  LIST TYPES
        // ---------------------------------------------------------------------
        public static UParam StringList(string key, IEnumerable<string> values) =>
            Create(key, "list<string>", new List<string>(values));

        public static UParam IntList(string key, IEnumerable<int> values) =>
            Create(key, "list<int>", new List<int>(values));

        // ---------------------------------------------------------------------
        //  IDENTITY TYPES
        // ---------------------------------------------------------------------
        public static UParam Identity(string key, string identityId) =>
            Create(key, "identity", identityId);

        public static UParam IdentityObject(string key, UPSIdentityDescriptor descriptor) =>
            Create(key, "identityObject", descriptor);

        public static UParam IdentityList(string key, IEnumerable<UPSIdentityDescriptor> list) =>
            Create(key, "identityList", new List<UPSIdentityDescriptor>(list));

        // ---------------------------------------------------------------------
        //  JSON / OBJECT TYPES
        // ---------------------------------------------------------------------
        public static UParam Json(string key, object value)
        {
            // serialize to JsonElement so it stays typed
            var json = JsonSerializer.SerializeToElement(value);
            return Create(key, "json", json);
        }

        public static UParam Object<T>(string key, T obj) where T : class =>
            Create(key, typeof(T).Name, obj);

        // ---------------------------------------------------------------------
        //  NULL VALUES
        // ---------------------------------------------------------------------
        public static UParam Null(string key, string type = "null") =>
            Create(key, type, null);

        // ---------------------------------------------------------------------
        //  RAW (Advanced / Internal Use)
        // ---------------------------------------------------------------------
        public static UParam Raw(string key, object? value) =>
            Create(key, value?.GetType().Name ?? "unknown", value);
    }
}
