// ─────────────────────────────────────────────────────────────────────────────
//  Class     : VeniceProtocolAdapter
//  Namespace : AVA.Nomi.Bridge
//  Purpose   : OpenAI-compatible adapter for Venice API.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.Nomi.Bridge;

public class VeniceProtocolAdapter : OpenAiCompatibleProtocolAdapter
{
    private readonly string _ApiKey;

    public override string ProtocolName => "venice-http";

    protected override string BaseUrl => "https://api.venice.ai";

    protected override string ApiKey => _ApiKey;

    public VeniceProtocolAdapter(string apiKey)
    {
        _ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    protected override string GetSourceName() => "Venice";
}
