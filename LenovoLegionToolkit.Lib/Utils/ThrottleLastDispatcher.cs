using System;
using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Utils;

/// <summary>
/// 限制执行频率的调度器，在指定的间隔时间内只执行最后一次调用的任务。
/// </summary>
public class ThrottleLastDispatcher : IDisposable
{
    private readonly TimeSpan _interval;
    private readonly string? _tag;
    private readonly object _lock = new();
    private long _currentVersion;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="ThrottleLastDispatcher"/> 类的新实例。
/// </summary>
    /// <param name="interval">节流的时间间隔。</param>
    /// <param name="tag">用于日志记录的可选标签。</param>
    /// <exception cref="ArgumentOutOfRangeException">当间隔时间小于零时抛出。</exception>
    public ThrottleLastDispatcher(TimeSpan interval, string? tag = null)
    {
        if (interval < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval));

        _interval = interval;
        _tag = tag;
    }

    /// <summary>
    /// 调度一个任务。如果在 <see cref="interval"/> 内有新的任务到达，之前的任务将被取消。
    /// </summary>
    /// <param name="task">要执行的任务。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <exception cref="ArgumentNullException">当任务为空时抛出。</exception>
    /// <exception cref="ObjectDisposedException">当调度器已释放时抛出。</exception>
    public async Task DispatchAsync(Func<Task> task)
    {
        ArgumentNullException.ThrowIfNull(task);

        long myVersion;
        CancellationTokenSource cts;
        lock (_lock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ThrottleLastDispatcher));

            myVersion = ++_currentVersion;
            
            // Cancel previous delay
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            _cancellationTokenSource = new CancellationTokenSource();
            cts = _cancellationTokenSource;
        }

        try
        {
            if (_interval > TimeSpan.Zero)
                await Task.Delay(_interval, cts.Token).ConfigureAwait(false);

            lock (_lock)
            {
                if (myVersion != _currentVersion)
                    return;
            }

            if (_tag is not null && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Allowing... [tag={_tag}]");

            await task().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (_tag is not null && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Throttling... [tag={_tag}]");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            _currentVersion++;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
