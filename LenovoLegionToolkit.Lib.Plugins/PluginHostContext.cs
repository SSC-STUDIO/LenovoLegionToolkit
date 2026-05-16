using System.Threading;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Global plugin host context entry point.
/// </summary>
public static class PluginHostContext
{
    private static IPluginHostContext _current = NoOpPluginHostContext.Instance;

    /// <summary>
    /// Currently active host context. Always returns a non-null no-op fallback.
    /// </summary>
    public static IPluginHostContext Current => Volatile.Read(ref _current);

    /// <summary>
    /// Sets the current host context. Passing <c>null</c> restores the no-op fallback.
    /// </summary>
    public static void SetCurrent(IPluginHostContext? context) => Volatile.Write(ref _current, context ?? NoOpPluginHostContext.Instance);

    /// <summary>
    /// Restores the default no-op host context.
    /// </summary>
    public static void Reset() => SetCurrent(null);

    private sealed class NoOpPluginHostContext : IPluginHostContext
    {
        public static NoOpPluginHostContext Instance { get; } = new();

        public PluginHostMode Mode => PluginHostMode.Preview;

        public bool AllowSystemActions => false;

        public object? OwnerWindow => null;

        public bool OpenPluginSettings(string pluginId) => false;

        public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null) => null;
    }
}
