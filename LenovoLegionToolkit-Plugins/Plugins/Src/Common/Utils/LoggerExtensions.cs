using Microsoft.Extensions.Logging;

namespace LenovoLegionToolkit.Plugins.Common.Utils;

/// <summary>
/// Logger extension methods for plugins
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Log information with plugin context
    /// </summary>
    public static void LogPluginInfo<T>(this ILogger<T> logger, string pluginName, string message)
    {
        logger.LogInformation("[{Plugin}] {Message}", pluginName, message);
    }

    /// <summary>
    /// Log warning with plugin context
    /// </summary>
    public static void LogPluginWarning<T>(this ILogger<T> logger, string pluginName, string message)
    {
        logger.LogWarning("[{Plugin}] {Message}", pluginName, message);
    }

    /// <summary>
    /// Log error with plugin context
    /// </summary>
    public static void LogPluginError<T>(this ILogger<T> logger, string pluginName, string message, Exception? exception = null)
    {
        if (exception != null)
            logger.LogError(exception, "[{Plugin}] {Message}", pluginName, message);
        else
            logger.LogError("[{Plugin}] {Message}", pluginName, message);
    }

    /// <summary>
    /// Log debug with plugin context
    /// </summary>
    public static void LogPluginDebug<T>(this ILogger<T> logger, string pluginName, string message)
    {
        logger.LogDebug("[{Plugin}] {Message}", pluginName, message);
    }
}
