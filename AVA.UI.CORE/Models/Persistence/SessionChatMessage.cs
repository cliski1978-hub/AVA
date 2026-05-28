namespace AVA.UI.CORE.Models.Persistence;

/// <summary>
/// A single fully-recoverable message in a session chat log.
/// Stores display content, routing metadata, tool calls, and response details.
/// </summary>
public class SessionChatMessage
{
    /// <summary>Set by IAvaIdService.NewMessageId() — never assign Guid.NewGuid() directly.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>user | assistant | system | tool_call | tool_result</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Full message content — not truncated for display.</summary>
    public string Content { get; set; } = string.Empty;

    // ── Routing metadata ──────────────────────────────────────────────────────
    public string? ModelId { get; set; }
    public string? ModelLabel { get; set; }
    public string? ProfileId { get; set; }
    public string? TurnId { get; set; }

    // ── Timestamps ────────────────────────────────────────────────────────────
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // ── Error flag ────────────────────────────────────────────────────────────
    public bool IsError { get; set; }
    public string? ErrorCode { get; set; }

    // ── Tool calls ────────────────────────────────────────────────────────────
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }

    /// <summary>JSON-serialised tool call arguments.</summary>
    public string? ToolArguments { get; set; }

    /// <summary>JSON-serialised tool call result.</summary>
    public string? ToolResult { get; set; }

    // ── Response metadata ─────────────────────────────────────────────────────
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public string? StopReason { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
