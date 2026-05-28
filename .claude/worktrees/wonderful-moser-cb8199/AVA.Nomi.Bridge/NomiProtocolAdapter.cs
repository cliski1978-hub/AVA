using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Transport;
using AVA.UPS.Adapter.Utils;

namespace AVA.Nomi.Bridge;

public class NomiProtocolAdapter : IProtocolAdapter
{
    public string ProtocolName => "nomi-http";

    private NomiApiClient? _client;
    private string? _nomiId;

    public Task InitializeAsync(object? config = null)
    {
        if (config is NomiAdapterConfig cfg)
        {
            _client = cfg.Client;
            _nomiId = cfg.NomiId;
        }
        else if (config is NomiApiClient client)
        {
            _client = client;
            _nomiId = null;
        }
        else
        {
            throw new ArgumentException(
                $"{nameof(NomiProtocolAdapter)} requires a {nameof(NomiAdapterConfig)} or {nameof(NomiApiClient)} as config.");
        }

        return Task.CompletedTask;
    }

    public async Task<byte[]> SendAsync(byte[] requestBytes, CancellationToken ct = default)
    {
        if (_client is null)
            throw new InvalidOperationException($"{nameof(NomiProtocolAdapter)} has not been initialized.");

        var envelope = UPSJsonSerializer.DeserializeFromBytes<UPSMessageEnvelope>(requestBytes)
                       ?? throw new InvalidOperationException("Failed to deserialize inbound UPSMessageEnvelope.");

        var userParam = envelope.Payload?.FirstOrDefault(p => p.Key == "userMessage")
                        ?? throw new InvalidOperationException("Inbound envelope missing required UParam key='userMessage'.");

        var userMessage = userParam.Value?.ToString()
                          ?? throw new InvalidOperationException("UParam 'userMessage' has a null value.");

        var rawId = envelope.Payload?.FirstOrDefault(p => p.Key == "nomiId")?.Value?.ToString()
                    ?? _nomiId
                    ?? throw new InvalidOperationException("No nomiId available.");

        string nomiId;
        string? roomId;

        if (rawId.StartsWith("room:"))
        {
            // room:{roomUuid}:nomi:{nomiUuid}
            var Parts = rawId.Split(':');
            roomId  = Parts.Length >= 2 ? Parts[1] : null;
            nomiId  = Parts.Length >= 4 ? Parts[3] : rawId;
        }
        else
        {
            // nomi:{uuid} or bare uuid
            nomiId = rawId.StartsWith("nomi:") ? rawId[5..] : rawId;
            roomId = envelope.Payload?.FirstOrDefault(p => p.Key == "roomId")?.Value?.ToString();
        }

        string replyText;

        if (!string.IsNullOrEmpty(roomId))
        {
            // ── Room chat — send message then request specific Nomi to reply ──
            await _client.SendRoomChatAsync(roomId, userMessage, ct);
            var roomReply = await _client.RequestRoomReplyAsync(roomId, nomiId, ct);
            replyText = roomReply.ReplyMessage?.Text ?? string.Empty;
        }
        else
        {
            // ── Direct Nomi chat ───────────────────────────────────────────────
            var response = await _client.SendNomiChatAsync(nomiId, userMessage, ct);
            replyText = response.ReplyMessage?.Text ?? string.Empty;
        }

        var responseEnvelope = new UPSMessageEnvelope
        {
            Source = "Nomi",
            Target = envelope.Source,
            TargetMethod = envelope.TargetMethod,
            CorrelationId = envelope.CorrelationId,
            Timestamp = DateTime.UtcNow,
            Payload = new List<UParam>
            {
                UParamFactory.String("assistantMessage", replyText)
            }
        };

        return UPSJsonSerializer.SerializeToBytes(responseEnvelope);
    }
}

public class NomiAdapterConfig
{
    public NomiApiClient Client { get; set; } = default!;
    public string NomiId { get; set; } = default!;
}