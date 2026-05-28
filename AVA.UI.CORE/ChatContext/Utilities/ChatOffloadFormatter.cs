using System.Text.Json;
using System.Text.Json.Serialization;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Utilities
{
    /// <summary>
    /// Formats chat offload packages as stable, searchable JSON.
    /// </summary>
    public static class ChatOffloadFormatter
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        static ChatOffloadFormatter()
        {
            JsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <summary>
        /// Serializes a chat offload package to indented JSON.
        /// </summary>
        public static string ToJson(ChatOffloadPackage package)
        {
            package ??= new ChatOffloadPackage();
            package.Messages ??= new List<ChatOffloadMessage>();
            package.Metadata ??= new Dictionary<string, string>();
            return JsonSerializer.Serialize(package, JsonOptions);
        }
    }
}
