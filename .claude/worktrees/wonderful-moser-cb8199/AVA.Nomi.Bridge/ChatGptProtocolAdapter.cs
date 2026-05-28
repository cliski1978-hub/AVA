// ─────────────────────────────────────────────────────────────────────────────
//  Class     : ChatGptProtocolAdapter
//  Namespace : AVA.Nomi.Bridge
//  Purpose   : OpenAI-compatible adapter for ChatGPT API.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.Nomi.Bridge;

public class ChatGptProtocolAdapter : OpenAiCompatibleProtocolAdapter
{
    private readonly string _ApiKey;

    public override string ProtocolName => "openai-http";

    protected override string BaseUrl => "https://api.openai.com";

    protected override string ApiKey => _ApiKey;

    public ChatGptProtocolAdapter(string apiKey)
    {
        _ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    protected override string GetSourceName() => "ChatGPT";
}
