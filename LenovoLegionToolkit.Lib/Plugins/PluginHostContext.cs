using System.Threading;

namespace LenovoLegionToolkit.Lib.Plugins;

public enum PluginHostMode
{
    Preview = 0,
    RealRuntime = 1,
}

public static class PluginHostContext
{
    private static IPluginHostContext _current = NoOpPluginHostContext.Instance;

    public static IPluginHostContext Current => Volatile.Read(ref _current);

    public static void SetCurrent(IPluginHostContext? context) => Volatile.Write(ref _current, context ?? NoOpPluginHostContext.Instance);

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
