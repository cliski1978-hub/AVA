namespace AVA.UPS.Adapter.Interfaces;

public interface IRoster
{
    IEnumerable<IModelEntry> GetModelEntries();
    IRouteTarget? ParseModelId(string modelId);
    Task LoadAsync(CancellationToken ct = default);
}