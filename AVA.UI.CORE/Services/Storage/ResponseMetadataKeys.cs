namespace AVA.UI.CORE.Services.Storage;

/// <summary>
/// Standard key constants for ChatSessionMessage.ResponseMetadata.
/// Use these instead of magic strings when storing assistant response metadata.
/// </summary>
public static class ResponseMetadataKeys
{
    // ── Model / Profile ───────────────────────────────────────────────────────
    public const string ModelId        = "modelId";
    public const string ModelProfileId = "modelProfileId";
    public const string Provider       = "provider";

    // ── Completion ────────────────────────────────────────────────────────────
    public const string FinishReason = "finishReason";
    public const string StopSequence = "stopSequence";

    // ── Tokens ────────────────────────────────────────────────────────────────
    public const string InputTokens  = "inputTokens";
    public const string OutputTokens = "outputTokens";
    public const string TotalTokens  = "totalTokens";

    // ── Latency ───────────────────────────────────────────────────────────────
    public const string LatencyMs       = "latencyMs";
    public const string RequestStartUtc = "requestStartUtc";
    public const string RequestEndUtc   = "requestEndUtc";

    // ── Routing ───────────────────────────────────────────────────────────────
    public const string RoutingDecision  = "routingDecision";
    public const string ToolUseDecision  = "toolUseDecision";
    public const string BroadcastTurnId  = "broadcastTurnId";

    // ── Raw response ──────────────────────────────────────────────────────────
    public const string RawResponseJson = "rawResponseJson";
    public const string RequestId       = "requestId";
}
