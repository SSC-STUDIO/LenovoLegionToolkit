using System;
using System.Diagnostics;

namespace LenovoLegionToolkit.Lib.Utils;

public class SafePerformanceCounter(string categoryName, string counterName, string instanceName) : IDisposable
{
    private PerformanceCounter? _performanceCounter;
    private bool _disposed;

    public float NextValue()
    {
        if (_disposed) return 0f;

        try
        {
            TryCreateIfNeeded();
            return _performanceCounter?.NextValue() ?? 0f;
        }
        catch
        {
            return 0f;
        }
    }

    public void Reset()
    {
        if (_disposed) return;

        _performanceCounter?.Dispose();
        _performanceCounter = null;
        _ = NextValue();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _performanceCounter?.Dispose();
        _performanceCounter = null;
        GC.SuppressFinalize(this);
    }

    private void TryCreateIfNeeded()
    {
        if (_performanceCounter is not null)
            return;

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Creating performance counter. [categoryName={categoryName}, counterName={counterName}, instanceName={instanceName}]");

            _performanceCounter = new(categoryName, counterName, instanceName);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to create performance counter. [categoryName={categoryName}, counterName={counterName}, instanceName={instanceName}]", ex);

            _performanceCounter = null;
        }
    }
}
