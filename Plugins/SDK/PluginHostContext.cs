using System;
using System.IO;
using System.Reflection;

namespace LenovoLegionToolkit.Plugins.SDK;

/// <summary>
/// Provides access to the current plugin host and preserves compatibility with both
/// the new host-context API and older released LLT/UDT builds.
/// </summary>
public static class PluginHostContext
{
    private const string HostLibAssemblyName = "LenovoLegionToolkit.Lib";
    private const string HostUdtLibAssemblyName = "UniversalDeviceToolkit.Lib";
    private const string HostWpfAssemblyName = "Lenovo Legion Toolkit";
    private const string HostUdtWpfAssemblyName = "Universal Device Toolkit";
    private const string HostPluginHostContextTypeName = "LenovoLegionToolkit.Lib.Plugins.PluginHostContext";
    private const string HostPluginSettingsWindowTypeName = "LenovoLegionToolkit.WPF.Windows.Settings.PluginSettingsWindow";
    private const string HostUdtPluginSettingsWindowTypeName = "UniversalDeviceToolkit.WPF.Windows.Settings.PluginSettingsWindow";
    private const string WpfApplicationTypeName = "System.Windows.Application";
    private const string WpfWindowTypeName = "System.Windows.Window";
    private const string WpfWindowStartupLocationTypeName = "System.Windows.WindowStartupLocation";

    private static readonly IPluginHostContext DefaultContext = new DefaultPluginHostContext();
    private static readonly object _contextLock = new object();
    private static IPluginHostContext _current = DefaultContext;

    public static IPluginHostContext Current
    {
        get
        {
            lock (_contextLock)
            {
                if (!ReferenceEquals(_current, DefaultContext))
                    return _current;

                var bridge = TryResolveHostBridge();
                if (bridge is not null)
                {
                    _current = bridge;
                    return bridge;
                }
                return DefaultContext;
            }
        }
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static IDisposable Push(IPluginHostContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var previous = Current;
        Current = context;
        return new RestoreScope(previous);
    }

    public static void Reset()
    {
        Current = DefaultContext;
    }

    public static object? CreateHostWindow(string fullTypeName, params object[] constructorArguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);

        // Try UDT assembly first, then fall back to LLT assembly
        var type = ResolveType(fullTypeName, HostUdtWpfAssemblyName)
                   ?? ResolveType(fullTypeName, HostWpfAssemblyName);
        if (type is null)
            return null;

        try
        {
            return Activator.CreateInstance(type, constructorArguments);
        }
        catch (MissingMethodException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static IPluginHostContext? TryResolveHostBridge()
    {
        // Try UDT host first, then fall back to LLT host
        var hostContextType = ResolveType(HostPluginHostContextTypeName, HostUdtLibAssemblyName)
                              ?? ResolveType(HostPluginHostContextTypeName, HostLibAssemblyName);
        if (hostContextType is null)
            return null;

        var currentProperty = hostContextType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
        var hostContext = currentProperty?.GetValue(null);
        if (hostContext is null)
            return null;

        var bridgedContext = new BridgedHostContext(hostContext);
        return bridgedContext.IsActive ? bridgedContext : null;
    }

    private static Type? ResolveType(string fullTypeName, string assemblyName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                return assembly.GetType(fullTypeName, throwOnError: false, ignoreCase: false);
            }
            catch (TypeLoadException)
            {
                return null;
            }
            catch (FileLoadException)
            {
                return null;
            }
        }

        try
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            return assembly.GetType(fullTypeName, throwOnError: false, ignoreCase: false);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryShowDialog(object dialog)
    {
        var windowType = ResolveType(WpfWindowTypeName, "PresentationFramework");
        if (windowType is null || !windowType.IsInstanceOfType(dialog))
            return false;

        // Marshal to the UI thread if needed — WPF dialogs require Dispatcher affinity
        var dispatcherType = ResolveType("System.Windows.Threading.Dispatcher", "WindowsBase");
        if (dispatcherType is not null)
        {
            try
            {
                var applicationType = ResolveType(WpfApplicationTypeName, "PresentationFramework");
                var currentApp = applicationType?.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (currentApp is not null)
                {
                    var dispatcherProperty = applicationType?.GetProperty("Dispatcher", BindingFlags.Public | BindingFlags.Instance);
                    var dispatcher = dispatcherProperty?.GetValue(currentApp);
                    if (dispatcher is not null)
                    {
                        var checkAccessMethod = dispatcherType.GetMethod("CheckAccess", BindingFlags.Public | BindingFlags.Instance);
                        var checkResult = checkAccessMethod?.Invoke(dispatcher, null);
                        if (checkResult is false)
                        {
                            var invokeMethod = dispatcherType.GetMethod("Invoke", [typeof(Action)]);
                            invokeMethod?.Invoke(dispatcher, [new Action(() => TryShowDialog(dialog))]);
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Fall through to direct attempt
            }
        }

        try
        {
            var ownerWindow = ResolveOwnerWindow(windowType);
            if (ownerWindow is not null && !ReferenceEquals(ownerWindow, dialog))
            {
                var ownerProperty = windowType.GetProperty("Owner", BindingFlags.Public | BindingFlags.Instance);
                if (ownerProperty?.CanWrite == true)
                    ownerProperty.SetValue(dialog, ownerWindow);
            }

            var startupLocationType = ResolveType(WpfWindowStartupLocationTypeName, "PresentationFramework");
            var startupLocationProperty = windowType.GetProperty("WindowStartupLocation", BindingFlags.Public | BindingFlags.Instance);
            if (startupLocationType is not null && startupLocationProperty?.CanWrite == true)
            {
                var centerOwner = Enum.Parse(startupLocationType, "CenterOwner");
                startupLocationProperty.SetValue(dialog, centerOwner);
            }

            var showDialogMethod = windowType.GetMethod("ShowDialog", BindingFlags.Public | BindingFlags.Instance);
            showDialogMethod?.Invoke(dialog, null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static object? ResolveOwnerWindow(Type windowType)
    {
        var applicationType = ResolveType(WpfApplicationTypeName, "PresentationFramework");
        var currentApplication = ResolveCurrentApplication(applicationType);
        if (currentApplication is null)
            return null;

        if (applicationType is null)
            return null;

        var mainWindowProperty = applicationType.GetProperty("MainWindow", BindingFlags.Public | BindingFlags.Instance);
        var mainWindow = mainWindowProperty?.GetValue(currentApplication);
        return mainWindow is not null && windowType.IsInstanceOfType(mainWindow) ? mainWindow : null;
    }

    private static object? ResolveCurrentApplication(Type? applicationType)
    {
        var currentProperty = applicationType?.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
        return currentProperty?.GetValue(null);
    }

    private static object? TryReadProperty(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(target);
    }

    private static bool TryInvokeBoolMethod(object target, string methodName, params object?[] arguments)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method is null)
            return false;

        try
        {
            var result = method.Invoke(target, arguments);
            if (result is bool boolResult)
                return boolResult;

            return false;
        }
        catch
        {
            return false;
        }
    }

    private sealed class DefaultPluginHostContext : IPluginHostContext
    {
        private bool HasLegacyHostRuntime =>
            ResolveType(HostUdtPluginSettingsWindowTypeName, HostUdtWpfAssemblyName) is not null &&
            ResolveCurrentApplication(ResolveType(WpfApplicationTypeName, "PresentationFramework")) is not null;

        public PluginHostMode Mode => HasLegacyHostRuntime ? PluginHostMode.RealRuntime : PluginHostMode.Preview;

        public bool AllowSystemActions => HasLegacyHostRuntime;

        public object? OwnerWindow
        {
            get
            {
                var windowType = ResolveOwnerWindowType();
                return windowType is null ? null : ResolveOwnerWindow(windowType);
            }
        }

        public bool OpenPluginSettings(string pluginId)
        {
            if (!HasLegacyHostRuntime || string.IsNullOrWhiteSpace(pluginId))
                return false;

            var dialog = CreateHostWindow(HostUdtPluginSettingsWindowTypeName, pluginId)
                         ?? CreateHostWindow(HostPluginSettingsWindowTypeName, pluginId);
            return dialog is not null && TryShowDialog(dialog);
        }

        public bool ShowDialog(object dialogOrContent, string? title = null, string? icon = null)
        {
            if (!HasLegacyHostRuntime || dialogOrContent is null)
                return false;

            return TryShowDialog(dialogOrContent);
        }

        private static Type? ResolveOwnerWindowType() => ResolveType(WpfWindowTypeName, "PresentationFramework");
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly IPluginHostContext _previous;
        private bool _disposed;

        public RestoreScope(IPluginHostContext previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Current = _previous;
            _disposed = true;
        }
    }

    private sealed class BridgedHostContext : IPluginHostContext
    {
        private readonly object _hostContext;

        public BridgedHostContext(object hostContext)
        {
            _hostContext = hostContext;
        }

        public bool IsActive => AllowSystemActions || OwnerWindow is not null || Mode != PluginHostMode.Preview;

        public PluginHostMode Mode
        {
            get
            {
                try
                {
                    var modeValue = TryReadProperty(_hostContext, "Mode")?.ToString();
                    return string.Equals(modeValue, "RealRuntime", StringComparison.OrdinalIgnoreCase)
                        ? PluginHostMode.RealRuntime
                        : PluginHostMode.Preview;
                }
                catch
                {
                    return PluginHostMode.Preview;
                }
            }
        }

        public bool AllowSystemActions => TryReadProperty(_hostContext, "AllowSystemActions") is bool allowSystemActions && allowSystemActions;

        public object? OwnerWindow => TryReadProperty(_hostContext, "OwnerWindow");

        public bool OpenPluginSettings(string pluginId) => TryInvokeBoolMethod(_hostContext, "OpenPluginSettings", pluginId);

        public bool ShowDialog(object dialogOrContent, string? title = null, string? icon = null) =>
            TryInvokeBoolMethod(_hostContext, "ShowDialog", dialogOrContent, title, icon);
    }
}
