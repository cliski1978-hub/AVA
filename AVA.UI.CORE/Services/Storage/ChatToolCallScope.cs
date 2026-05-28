using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Models.Chat;

namespace AVA.UI.CORE.Services.Storage;

/// <summary>
/// Captures the full lifecycle of a single tool call:
/// ToolCallId, ToolName, ArgumentsJson, ResultJson,
/// StartedUtc, CompletedUtc, Succeeded, ErrorMessage.
///
/// Usage:
///   var scope = ChatToolCallScope.Begin(ids, "search_vault", argsJson);
///   // ... execute tool ...
///   var log = scope.Complete(resultJson);      // success
///   var log = scope.Fail("not found");         // failure
/// </summary>
public sealed class ChatToolCallScope
{
    private readonly ChatToolCallLog _log;

    private ChatToolCallScope(ChatToolCallLog log)
    {
        _log = log;
    }

    /// <summary>
    /// Starts timing a tool call. Call this before invoking the tool.
    /// </summary>
    public static ChatToolCallScope Begin(IAvaIdService ids, string toolName, string argumentsJson)
    {
        var log = new ChatToolCallLog
        {
            ToolCallId    = ids.NewToolCallId(),
            ToolName      = toolName,
            ArgumentsJson = argumentsJson,
            StartedUtc    = DateTime.UtcNow,
            CompletedUtc  = DateTime.UtcNow
        };

        return new ChatToolCallScope(log);
    }

    /// <summary>
    /// Completes a successful tool call. Returns the fully-populated log entry.
    /// </summary>
    public ChatToolCallLog Complete(string resultJson)
    {
        _log.ResultJson   = resultJson;
        _log.CompletedUtc = DateTime.UtcNow;
        _log.Succeeded    = true;
        _log.ErrorMessage = string.Empty;
        return _log;
    }

    /// <summary>
    /// Completes a failed tool call. Returns the fully-populated log entry.
    /// </summary>
    public ChatToolCallLog Fail(string errorMessage)
    {
        _log.ResultJson   = string.Empty;
        _log.CompletedUtc = DateTime.UtcNow;
        _log.Succeeded    = false;
        _log.ErrorMessage = errorMessage;
        return _log;
    }

    /// <summary>
    /// The tool call ID — use this to attach the log to a specific message.
    /// </summary>
    public string ToolCallId => _log.ToolCallId;
}
