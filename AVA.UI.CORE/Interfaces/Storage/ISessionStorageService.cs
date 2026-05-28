namespace AVA.UI.CORE.Interfaces.Storage
{
    public interface ISessionStorageService
    {
        Task SetAsync<T>(string key, T value);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);
        Task ClearAsync();
    }
}
