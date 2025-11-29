using System;
using System.Threading.Tasks;

namespace AICalendar.Application.Common.Interfaces;

/// <summary>
/// Interface for caching service
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get an item from cache
    /// </summary>
    /// <typeparam name="T">Type of the item</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>The item if found, otherwise default</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Set an item in cache
    /// </summary>
    /// <typeparam name="T">Type of the item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Item to cache</param>
    /// <param name="expiration">Expiration time</param>
    Task SetAsync<T>(string key, T value, TimeSpan expiration);

    /// <summary>
    /// Remove an item from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Get an item from cache, or create it if it doesn't exist
    /// </summary>
    /// <typeparam name="T">Type of the item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Function to create the item if not found</param>
    /// <param name="expiration">Expiration time if created</param>
    /// <returns>The cached or created item</returns>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
}
