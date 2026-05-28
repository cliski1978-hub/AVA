using AVA.UI.CORE.Models.Chat;

namespace AVA.UI.CORE.Services.Storage;

/// <summary>
/// Fluent builder for ChatSessionMessage.ResponseMetadata.
/// Ensures consistent key usage and avoids magic strings at call sites.
///
/// Usage:
///   var message = new ChatSessionMessage { ... };
///   ResponseMetadataBuilder
///       .For(message)
///       .WithModel("claude-sonnet-4", "profile-1", "Anthropic")
///       .WithTokens(inputTokens: 512, outputTokens: 256)
///       .WithLatency(startUtc, endUtc)
///       .WithFinishReason("end_turn")
///       .Build();
/// </summary>
public sealed class ResponseMetadataBuilder
{
    private readonly ChatSessionMessage _message;

    private ResponseMetadataBuilder(ChatSessionMessage message)
    {
        _message = message;
        _message.ResponseMetadata ??= new Dictionary<string, object>();
    }

    public static ResponseMetadataBuilder For(ChatSessionMessage message)
        => new(message);

    // ── Model / Profile ───────────────────────────────────────────────────────
    public ResponseMetadataBuilder WithModel(string modelId, string? modelProfileId = null, string? provider = null)
    {
        Set(ResponseMetadataKeys.ModelId, modelId);
        _message.ModelId        = modelId;
        _message.ModelProfileId = modelProfileId ?? string.Empty;

        if (modelProfileId != null) Set(ResponseMetadataKeys.ModelProfileId, modelProfileId);
        if (provider       != null) Set(ResponseMetadataKeys.Provider, provider);
        return this;
    }

    // ── Completion ────────────────────────────────────────────────────────────
    public ResponseMetadataBuilder WithFinishReason(string reason)
    {
        Set(ResponseMetadataKeys.FinishReason, reason);
        return this;
    }

    // ── Tokens ────────────────────────────────────────────────────────────────
    public ResponseMetadataBuilder WithTokens(int inputTokens, int outputTokens)
    {
        Set(ResponseMetadataKeys.InputTokens,  inputTokens);
        Set(ResponseMetadataKeys.OutputTokens, outputTokens);
        Set(ResponseMetadataKeys.TotalTokens,  inputTokens + outputTokens);
        return this;
    }

    // ── Latency ───────────────────────────────────────────────────────────────
    public ResponseMetadataBuilder WithLatency(DateTime requestStartUtc, DateTime requestEndUtc)
    {
        var ms = (requestEndUtc - requestStartUtc).TotalMilliseconds;
        Set(ResponseMetadataKeys.LatencyMs,       (long)ms);
        Set(ResponseMetadataKeys.RequestStartUtc, requestStartUtc.ToString("O"));
        Set(ResponseMetadataKeys.RequestEndUtc,   requestEndUtc.ToString("O"));
        return this;
    }

    // ── Routing ───────────────────────────────────────────────────────────────
    public ResponseMetadataBuilder WithRoutingDecision(string decision)
    {
        Set(ResponseMetadataKeys.RoutingDecision, decision);
        return this;
    }

    public ResponseMetadataBuilder WithToolUseDecision(string decision)
    {
        Set(ResponseMetadataKeys.ToolUseDecision, decision);
        return this;
    }

    public ResponseMetadataBuilder WithBroadcastTurnId(string turnId)
    {
        Set(ResponseMetadataKeys.BroadcastTurnId, turnId);
        return this;
    }

    // ── Raw response ──────────────────────────────────────────────────────────
    public ResponseMetadataBuilder WithRawResponse(string rawJson, string? requestId = null)
    {
        Set(ResponseMetadataKeys.RawResponseJson, rawJson);
        if (requestId != null) Set(ResponseMetadataKeys.RequestId, requestId);
        return this;
    }

    // ── Custom key ────────────────────────────────────────────────────────────
    public ResponseMetadataBuilder With(string key, object value)
    {
        Set(key, value);
        return this;
    }

    /// <summary>Finalises the builder — returns the message for chaining.</summary>
    public ChatSessionMessage Build() => _message;

    private void Set(string key, object value)
        => _message.ResponseMetadata[key] = value;
}
