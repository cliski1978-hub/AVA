// ─────────────────────────────────────────────────────────────────────────────
//  Class     : DeepSeekProtocolAdapter
//  Namespace : AVA.Nomi.Bridge
//  Purpose   : OpenAI-compatible adapter for DeepSeek API.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.Nomi.Bridge;

public class DeepSeekProtocolAdapter : OpenAiCompatibleProtocolAdapter
{
    private readonly string _ApiKey;

    public override string ProtocolName => "deepseek-http";

    protected override string BaseUrl => "https://api.deepseek.com";

    protected override string ApiKey => _ApiKey;

    public DeepSeekProtocolAdapter(string apiKey)
    {
        _ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    protected override string GetSourceName() => "DeepSeek";
}
