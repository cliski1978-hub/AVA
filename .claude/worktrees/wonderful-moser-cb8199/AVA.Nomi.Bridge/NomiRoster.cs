using System.Collections.Concurrent;
using AVA.UPS.Adapter.Interfaces;

namespace AVA.Nomi.Bridge;

public class NomiRoster : IRoster
{
    private readonly NomiApiClient _client;

    private ConcurrentDictionary<string, NomiApiClient.NomiRecord> _nomis = new();
    private ConcurrentDictionary<string, NomiApiClient.RoomRecord> _rooms = new();

    public IEnumerable<NomiApiClient.NomiRecord> Nomis => _nomis.Values;
    public IEnumerable<NomiApiClient.RoomRecord> Rooms => _rooms.Values;

    public NomiRoster(NomiApiClient client)
    {
        _client = client;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var nomis = await _client.GetNomisAsync(ct);

        List<NomiApiClient.RoomRecord> rooms;
        try
        {
            rooms = await _client.GetRoomsAsync(ct);
        }
        catch
        {
            rooms = new List<NomiApiClient.RoomRecord>();
        }

        var nomiDict = new ConcurrentDictionary<string, NomiApiClient.NomiRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in nomis)
            nomiDict[n.Uuid] = n;

        var roomDict = new ConcurrentDictionary<string, NomiApiClient.RoomRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rooms)
            roomDict[r.Uuid] = r;

        _nomis = nomiDict;
        _rooms = roomDict;

        Console.WriteLine($"[NomiRoster] Loaded {_nomis.Count} Nomis, {_rooms.Count} Rooms.");
    }

    public NomiApiClient.NomiRecord? GetNomi(string uuid) =>
        _nomis.TryGetValue(uuid, out var n) ? n : null;

    public NomiApiClient.RoomRecord? GetRoom(string uuid) =>
        _rooms.TryGetValue(uuid, out var r) ? r : null;

    public IEnumerable<IModelEntry> GetModelEntries()
    {
        var entries = new List<IModelEntry>();

        foreach (var nomi in _nomis.Values)
        {
            entries.Add(new ModelEntry
            {
                Id = $"nomi:{nomi.Uuid}",
                Label = $"{nomi.Name} (Direct)",
                Type = "nomi"
            });
        }

        foreach (var room in _rooms.Values)
        {
            foreach (var nomi in room.Nomis)
            {
                entries.Add(new ModelEntry
                {
                    Id = $"room:{room.Uuid}:nomi:{nomi.Uuid}",
                    Label = $"{room.Name} → {nomi.Name}",
                    Type = "room"
                });
            }
        }

        return entries;
    }

    public IRouteTarget? ParseModelId(string modelId)
    {
        if (modelId.StartsWith("nomi:"))
        {
            return new RouteTarget
            {
                IsRoom = false,
                NomiId = modelId[5..]
            };
        }

        if (modelId.StartsWith("room:"))
        {
            var parts = modelId.Split(':');
            if (parts.Length == 4)
            {
                return new RouteTarget
                {
                    IsRoom = true,
                    RoomId = parts[1],
                    NomiId = parts[3]
                };
            }
        }

        return null;
    }
}

public class ModelEntry : IModelEntry
{
    public string Id { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Type { get; set; } = default!;
}

public class RouteTarget : IRouteTarget
{
    public bool IsRoom { get; set; }
    public string? RoomId { get; set; }
    public string NomiId { get; set; } = default!;
}