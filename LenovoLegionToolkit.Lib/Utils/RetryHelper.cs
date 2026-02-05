using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Utils;

public class MaximumRetriesReachedException : Exception;

public static class RetryHelper
{
    public static async Task RetryAsync(Func<Task> action,
        int? maximumRetries = null,
        TimeSpan? timeout = null,
        Func<Exception, bool>? matchingException = null,
        IDelayProvider? delayProvider = null,
        [CallerMemberName] string? tag = null)
    {
        maximumRetries ??= 3;
        timeout ??= TimeSpan.Zero;
        matchingException ??= (_) => true;

        var retries = 0;
        var success = false;

        while (!success)
        {
            try
            {
                if (retries >= maximumRetries)
                    throw new MaximumRetriesReachedException();

                await action().ConfigureAwait(false);
                success = true;
            }
            catch (Exception ex)
            {
                if (!matchingException(ex))
                    throw;

                retries++;

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Retrying {retries}/{maximumRetries}... [tag={tag}]");

                if (delayProvider is not null)
                    await delayProvider.Delay(timeout.Value, CancellationToken.None).ConfigureAwait(false);
                else
                    await Task.Delay(timeout.Value).ConfigureAwait(false);
            }
        }
    }
}
