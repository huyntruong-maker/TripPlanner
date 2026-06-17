namespace Application.Interfaces.Caching;

public interface ICacheManager
{
    Task<T?> GetData<T>(string key);

    Task<bool> SetData<T>(string key, T data, TimeSpan duration);

    Task<bool> RemoveCache(string key);

    Task RemoveByPatternAsync(string pattern);
}