using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.System.Management;

public static class WMICache
{
    private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private static TimeSpan _defaultCacheDuration = TimeSpan.FromSeconds(30);

    public static void SetCacheDuration(TimeSpan duration)
    {
        _defaultCacheDuration = duration;
    }

    public static void Clear()
    {
        _cache.Clear();
    }

    public static void ClearByPrefix(string prefix)
    {
        foreach (var key in _cache.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            _cache.TryRemove(key, out _);
        }
    }

    public static async Task<bool> ExistsAsync(string scope, FormattableString query, TimeSpan? cacheDuration = null)
    {
        var cacheKey = GetCacheKey("Exists", scope, query);
        
        if (TryGetFromCache<bool>(cacheKey, out var cachedResult))
            return cachedResult;

        var result = await WMI.ExistsAsync(scope, query).ConfigureAwait(false);
        SetCache(cacheKey, result, cacheDuration ?? _defaultCacheDuration);
        return result;
    }

    public static async Task<IEnumerable<T>> ReadAsync<T>(string scope, FormattableString query, Func<PropertyDataCollection, T> converter, TimeSpan? cacheDuration = null)
    {
        var cacheKey = GetCacheKey("Read", scope, query);
        
        if (TryGetFromCache<IEnumerable<T>>(cacheKey, out var cachedResult))
            return cachedResult;

        var result = await WMI.ReadAsync(scope, query, converter).ConfigureAwait(false);
        SetCache(cacheKey, result, cacheDuration ?? _defaultCacheDuration);
        return result;
    }

    public static async Task CallAsync(string scope, FormattableString query, string methodName, Dictionary<string, object> methodParams)
    {
        await WMI.CallAsync(scope, query, methodName, methodParams).ConfigureAwait(false);
        
        var cacheKeyPrefix = GetCacheKeyPrefix(scope, query);
        ClearByPrefix(cacheKeyPrefix);
    }

    public static async Task<T> CallAsync<T>(string scope, FormattableString query, string methodName, Dictionary<string, object> methodParams, Func<PropertyDataCollection, T> converter, TimeSpan? cacheDuration = null)
    {
        var result = await WMI.CallAsync(scope, query, methodName, methodParams, converter).ConfigureAwait(false);
        
        var cacheKeyPrefix = GetCacheKeyPrefix(scope, query);
        ClearByPrefix(cacheKeyPrefix);
        
        return result;
    }

    private static string GetCacheKey(string operation, string scope, FormattableString query)
    {
        var queryStr = query.ToString(WMI.WMIPropertyValueFormatter.Instance);
        return $"{operation}_{scope}_{queryStr.GetHashCode():X8}";
    }

    private static string GetCacheKeyPrefix(string scope, FormattableString query)
    {
        var queryStr = query.ToString(WMI.WMIPropertyValueFormatter.Instance);
        return $"{scope}_{queryStr.GetHashCode():X8}";
    }

    private static bool TryGetFromCache<T>(string key, out T value)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.ExpiryTime)
            {
                if (entry.Value is T typedValue)
                {
                    value = typedValue;
                    return true;
                }
            }
            else
            {
                _cache.TryRemove(key, out _);
            }
        }

        value = default!;
        return false;
    }

    private static void SetCache<T>(string key, T value, TimeSpan duration)
    {
        var entry = new CacheEntry
        {
            Value = value ?? throw new ArgumentNullException(nameof(value)),
            ExpiryTime = DateTime.UtcNow + duration
        };
        _cache.AddOrUpdate(key, entry, (_, _) => entry);
    }

    private class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTime ExpiryTime { get; set; }
    }
}
