using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace LenovoLegionToolkit.Lib.Utils;

/// <summary>
/// 反射缓存助手，用于缓存PropertyInfo以减少反射开销。
/// </summary>
/// <remarks>
/// 在高频调用的场景下（如传感器数据采集），每次反射获取PropertyInfo都会产生开销。
/// 此类通过缓存PropertyInfo来优化性能。
/// </remarks>
public static class ReflectionCache
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyByNameCache = new();

    /// <summary>
    /// 获取类型的所有公共属性，使用缓存避免重复反射。
    /// </summary>
    /// <param name="type">要获取属性的类型。</param>
    /// <returns>属性信息数组。</returns>
    public static PropertyInfo[] GetCachedProperties(Type type)
    {
        return _propertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }

    /// <summary>
    /// 获取指定名称的属性，使用缓存避免重复反射。
    /// </summary>
    /// <param name="type">要获取属性的类型。</param>
    /// <param name="propertyName">属性名称。</param>
    /// <returns>属性信息，如果不存在则返回null。</returns>
    public static PropertyInfo? GetCachedProperty(Type type, string propertyName)
    {
        var key = (type, propertyName);
        return _propertyByNameCache.GetOrAdd(key, k =>
        {
            var props = GetCachedProperties(k.Item1);
            foreach (var prop in props)
            {
                if (prop.Name == k.Item2)
                    return prop;
            }
            return null;
        });
    }

    /// <summary>
    /// 获取属性值，使用缓存的PropertyInfo。
    /// </summary>
    /// <param name="obj">要获取属性值的对象。</param>
    /// <param name="propertyName">属性名称。</param>
    /// <returns>属性值，如果获取失败则返回null。</returns>
    public static object? GetCachedPropertyValue(object obj, string propertyName)
    {
        if (obj == null) return null;
        
        var prop = GetCachedProperty(obj.GetType(), propertyName);
        return prop?.GetValue(obj);
    }

    /// <summary>
    /// 清除所有缓存。
    /// </summary>
    public static void ClearCache()
    {
        _propertyCache.Clear();
        _propertyByNameCache.Clear();
    }
}

/// <summary>
/// GPU功耗信息获取的缓存助手。
/// </summary>
/// <remarks>
/// 用于缓存nvidia-smi调用结果和失败状态，避免频繁调用外部进程。
/// </remarks>
public class GPUPowerInfoCache
{
    private int _cachedWattage = -1;
    private double _cachedVoltage;
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private bool _nvidiaSmiFailed;
    private DateTime _nvidiaSmiLastAttempt = DateTime.MinValue;
    
    private readonly TimeSpan _cacheDuration;
    private readonly TimeSpan _nvidiaSmiRetryInterval;

    /// <summary>
    /// 初始化GPUPowerInfoCache的新实例。
    /// </summary>
    /// <param name="cacheDuration">缓存持续时间，默认5秒。</param>
    /// <param name="nvidiaSmiRetryInterval">nvidia-smi重试间隔，默认30秒。</param>
    public GPUPowerInfoCache(TimeSpan? cacheDuration = null, TimeSpan? nvidiaSmiRetryInterval = null)
    {
        _cacheDuration = cacheDuration ?? TimeSpan.FromSeconds(5);
        _nvidiaSmiRetryInterval = nvidiaSmiRetryInterval ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 获取缓存的功耗信息。
    /// </summary>
    /// <returns>功耗和电压的元组。</returns>
    public (int wattage, double voltage) GetCached()
    {
        return (_cachedWattage, _cachedVoltage);
    }

    /// <summary>
    /// 更新缓存。
    /// </summary>
    /// <param name="wattage">功耗（瓦特）。</param>
    /// <param name="voltage">电压。</param>
    public void Update(int wattage, double voltage)
    {
        _cachedWattage = wattage;
        _cachedVoltage = voltage;
        _lastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 检查缓存是否有效。
    /// </summary>
    /// <returns>如果缓存有效则返回true。</returns>
    public bool IsCacheValid()
    {
        return _cachedWattage >= 0 && (DateTime.UtcNow - _lastUpdateTime) < _cacheDuration;
    }

    /// <summary>
    /// 标记nvidia-smi调用失败。
    /// </summary>
    public void MarkNvidiaSmiFailed()
    {
        _nvidiaSmiFailed = true;
        _nvidiaSmiLastAttempt = DateTime.UtcNow;
    }

    /// <summary>
    /// 检查是否应该尝试nvidia-smi调用。
    /// </summary>
    /// <returns>如果应该尝试则返回true。</returns>
    public bool ShouldTryNvidiaSmi()
    {
        if (!_nvidiaSmiFailed) return true;
        return (DateTime.UtcNow - _nvidiaSmiLastAttempt) > _nvidiaSmiRetryInterval;
    }

    /// <summary>
    /// 重置nvidia-smi失败状态。
    /// </summary>
    public void ResetNvidiaSmiFailed()
    {
        _nvidiaSmiFailed = false;
    }
}
