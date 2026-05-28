using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AVA.UPS.Adapter.Utils
{
    /// <summary>
    /// Central JSON serializer for all UPS components.
    /// Ensures consistent JSON formatting across modules and transports.
    /// Adds byte[] support for DUAS adapter routing.
    /// </summary>
    public static class UPSJsonSerializer
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ---------------------------------------------------------
        // STRING SERIALIZATION (existing behavior)
        // ---------------------------------------------------------
        public static string Serialize<T>(T value)
            => JsonSerializer.Serialize(value, Options);

        public static T? Deserialize<T>(string json)
            => JsonSerializer.Deserialize<T>(json, Options);

        // ---------------------------------------------------------
        // BYTE[] SERIALIZATION (required for DUAS)
        // ---------------------------------------------------------
        public static byte[] SerializeToBytes<T>(T value)
            => JsonSerializer.SerializeToUtf8Bytes(value, Options);

        public static T? DeserializeFromBytes<T>(byte[] bytes)
            => JsonSerializer.Deserialize<T>(bytes, Options);

        // ---------------------------------------------------------
        // Lowest level byte → string and string → byte helpers
        // ---------------------------------------------------------
        public static byte[] ToBytes(string value)
            => Encoding.UTF8.GetBytes(value);

        public static string FromBytes(byte[] bytes)
            => Encoding.UTF8.GetString(bytes);
    }
}
