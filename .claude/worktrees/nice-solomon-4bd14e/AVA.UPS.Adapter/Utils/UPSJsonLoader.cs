using System;
using System.IO;
using System.Text.Json;

namespace AVA.UPS.Adapter.Utils
{
    /// <summary>
    /// Loads and deserializes JSON files using UPS-standard settings.
    /// </summary>
    public static class UPSJsonLoader
    {
        public static T Load<T>(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"UPS JSON file not found: {filePath}");

            var json = File.ReadAllText(filePath);
            return Deserialize<T>(json);
        }

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(
                json,
                UPSJsonSerializer.Options
            ) ?? throw new InvalidOperationException(
                $"Failed to deserialize JSON into {typeof(T).Name}");
        }
    }
}
