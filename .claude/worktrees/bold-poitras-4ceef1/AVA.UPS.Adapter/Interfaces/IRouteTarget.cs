namespace AVA.UPS.Adapter.Interfaces;

public interface IRouteTarget
{
    bool IsRoom { get; }
    string? RoomId { get; }
    string NomiId { get; }
}