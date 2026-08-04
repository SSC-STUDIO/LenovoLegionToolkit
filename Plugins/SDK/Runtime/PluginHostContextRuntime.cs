using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UniversalDeviceToolkit.Lib.Plugins;

namespace UniversalDeviceToolkit.Plugins.SDK;

/// <summary>
/// Provides access to the current plugin host and preserves compatibility with both
/// the new host-context API and older released LLT/UDT builds.  The class doubles as
/// the default <see cref="IPluginHostContext"/> implementation when no live host bridge
/// is available (preview / design-time scenarios).
/// </summary>
public class PluginHostContextRuntime : IPluginHostContext
{
    private const string HostLibAssemblyName = "UniversalDeviceToolkit.Lib";
    private const string HostUdtLibAssemblyName = "UniversalDeviceToolkit.Lib";
    private const string HostWpfAssemblyName = "Lenovo Legion Toolkit";
    private const string HostUdtWpfAssemblyName = "Universal Device Toolkit";
    private const string HostPluginHostContextTypeName = "UniversalDeviceToolkit.Lib.Plugins.PluginHostContext";
    private const string HostPluginSettingsWindowTypeName = "UniversalDeviceToolkit.WPF.Windows.Settings.PluginSettingsWindow";
    private const string HostUdtPluginSettingsWindowTypeName = "UniversalDeviceToolkit.WPF.Windows.Settings.PluginSettingsWindow";
    private const string WpfApplicationTypeName = "System.Windows.Application";
    private const string WpfWindowTypeName = "System.Windows.Window";
    private const string WpfWindowStartupLocationTypeName = "System.Windows.WindowStartupLocation";

    private static readonly ConcurrentDictionary<string, Type?> _typeCache = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyCache = new();
    private static readonly ConcurrentDictionary<(Type, string), MethodInfo?> _methodCache = new();

    private static readonly PluginHostContextRuntime DefaultContext = new();
    private static readonly object _contextLock = new();
    private static IPluginHostContext _current = DefaultContext;

    /// <summary>
    /// Gets or sets the current plugin host context.  On first access the runtime
    /// attempts to resolve a live host bridge via reflection; if none is found the
    /// default preview-oriented context is returned.
    /// </summary>
    public static IPluginHostContext Current
    {
        get
        {
            lock (_contextLock)
            {
                if (!ReferenceEquals(_current, DefaultContext))
                {
                    return _current;
                }

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

    /// <summary>
    /// Pushes a temporary context and returns a scope that restores the previous
    /// context on dispose.
    /// </summary>
    public static IDisposable Push(IPluginHostContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var previous = Current;
        Current = context;
        return new RestoreScope(previous);
    }

    /// <summary>
    /// Resets the current context back to the default runtime context.
    /// </summary>
    public static void Reset()
    {
        Current = DefaultContext;
    }

    /// <summary>
    /// Creates an instance of a host window type by full type name, trying the UDT
    /// assembly first and falling back to the legacy LLT assembly.
    /// </summary>
    public static object? CreateHostWindow(string fullTypeName, params object[] constructorArguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);

        // Try UDT assembly first, then fall back to legacy LLT assembly
        var type = ResolveType(fullTypeName, HostUdtWpfAssemblyName)
                   ?? ResolveType(fullTypeName, HostWpfAssemblyName);
        if (type is null)
        {
            return null;
        }

        try
        {
            return Activator.CreateInstance(type, constructorArguments);
        }
        catch (MissingMethodException)
        {
            return null;
        }
        catch (Exception ex)
        {
            // Assignable exceptions include TargetInvocationException (constructor body threw),
            // MemberAccessException, AmbiguousMatchException — these indicate genuine issues that
            // deserve observability rather than silent failure (BUGS.md H-007).
            Debug.WriteLine($"[SDK] CreateHostWindow(\"{fullTypeName}\") ctor threw: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static IPluginHostContext? TryResolveHostBridge()
    {
        // Try UDT host first, then fall back to legacy LLT host
        var hostContextType = ResolveType(HostPluginHostContextTypeName, HostUdtLibAssemblyName)
                              ?? ResolveType(HostPluginHostContextTypeName, HostLibAssemblyName);
        if (hostContextType is null)
        {
            return null;
        }

        var currentProperty = GetCachedProperty(hostContextType, "Current", BindingFlags.Public | BindingFlags.Static);
        var hostContext = currentProperty?.GetValue(null);
        if (hostContext is null)
        {
            return null;
        }

        var bridgedContext = new BridgedHostContext(hostContext);
        return bridgedContext.IsActive ? bridgedContext : null;
    }

    /// <summary>
    /// Resolves a <see cref="Type"/> by full name from a named assembly, using a
    /// cache so repeated lookups are free after the first call.
    /// </summary>
    private static Type? ResolveType(string fullTypeName, string assemblyName)
    {
        var key = assemblyName + "|" + fullTypeName;
        return _typeCache.GetOrAdd(key, _ => ResolveTypeUncached(fullTypeName, assemblyName));
    }

    private static Type? ResolveTypeUncached(string fullTypeName, string assemblyName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

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
        catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
        {
            // Expected: assembly DLL is absent or not a valid managed assembly — silent return.
            return null;
        }
        catch (Exception ex)
        {
            // Unexpected: FileLoadException (version mismatch / policy), SecurityException, ArgumentException…
            // Surface these via Debug.WriteLine so the failure is observable by a developer rather than
            // silently degrading to "Preview mode". See BUGS.md H-007.
            Debug.WriteLine($"[SDK] ResolveType(\"{fullTypeName}\", \"{assemblyName}\") failed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static PropertyInfo? GetCachedProperty(Type type, string name, BindingFlags bindingFlags)
    {
        return _propertyCache.GetOrAdd((type, name), _ => type.GetProperty(name, bindingFlags));
    }

    private static MethodInfo? GetCachedMethod(Type type, string name, BindingFlags bindingFlags)
    {
        return _methodCache.GetOrAdd((type, name), _ => type.GetMethod(name, bindingFlags));
    }

    private static bool? TryShowDialog(object dialog)
    {
        var windowType = ResolveType(WpfWindowTypeName, "PresentationFramework");
        if (windowType is null || !windowType.IsInstanceOfType(dialog))
        {
            return false;
        }

        // Marshal to the UI thread if needed — WPF dialogs require Dispatcher affinity
        var dispatcherType = ResolveType("System.Windows.Threading.Dispatcher", "WindowsBase");
        if (dispatcherType is not null)
        {
            try
            {
                var applicationType = ResolveType(WpfApplicationTypeName, "PresentationFramework");
                var currentApp = ResolveCurrentApplication(applicationType);
                if (currentApp is not null && applicationType is not null)
                {
                    var dispatcherProperty = GetCachedProperty(applicationType, "Dispatcher", BindingFlags.Public | BindingFlags.Instance);
                    var dispatcher = dispatcherProperty?.GetValue(currentApp);
                    if (dispatcher is not null)
                    {
                        var checkAccessMethod = GetCachedMethod(dispatcherType, "CheckAccess", BindingFlags.Public | BindingFlags.Instance);
                        var checkResult = checkAccessMethod?.Invoke(dispatcher, null);
                        if (checkResult is false)
                        {
                            var invokeMethod = _methodCache.GetOrAdd(
                                (dispatcherType, "Invoke|Action"),
                                _ => dispatcherType.GetMethod("Invoke", [typeof(Action)]));
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
                var ownerProperty = GetCachedProperty(windowType, "Owner", BindingFlags.Public | BindingFlags.Instance);
                if (ownerProperty?.CanWrite == true)
                {
                    ownerProperty.SetValue(dialog, ownerWindow);
                }
            }

            var startupLocationType = ResolveType(WpfWindowStartupLocationTypeName, "PresentationFramework");
            var startupLocationProperty = GetCachedProperty(windowType, "WindowStartupLocation", BindingFlags.Public | BindingFlags.Instance);
            if (startupLocationType is not null && startupLocationProperty?.CanWrite == true)
            {
                var centerOwner = Enum.Parse(startupLocationType, "CenterOwner");
                startupLocationProperty.SetValue(dialog, centerOwner);
            }

            var showDialogMethod = GetCachedMethod(windowType, "ShowDialog", BindingFlags.Public | BindingFlags.Instance);
            if (showDialogMethod is null)
            {
                return false;
            }

            var result = showDialogMethod.Invoke(dialog, null);
            return result as bool?;
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
        if (currentApplication is null || applicationType is null)
        {
            return null;
        }

        var mainWindowProperty = GetCachedProperty(applicationType, "MainWindow", BindingFlags.Public | BindingFlags.Instance);
        var mainWindow = mainWindowProperty?.GetValue(currentApplication);
        return mainWindow is not null && windowType.IsInstanceOfType(mainWindow) ? mainWindow : null;
    }

    private static object? ResolveCurrentApplication(Type? applicationType)
    {
        if (applicationType is null)
        {
            return null;
        }

        var currentProperty = GetCachedProperty(applicationType, "Current", BindingFlags.Public | BindingFlags.Static);
        return currentProperty?.GetValue(null);
    }

    private static object? TryReadProperty(object target, string propertyName)
    {
        var property = GetCachedProperty(target.GetType(), propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(target);
    }

    /// <summary>
    /// Invokes a method by name via reflection and interprets the result as a
    /// nullable <see cref="bool"/>.  Returns <c>null</c> when the method is missing
    /// or the return value is not a boolean.
    /// </summary>
    private static bool? TryInvokeBoolMethod(object target, string methodName, params object?[] arguments)
    {
        var method = GetCachedMethod(target.GetType(), methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method is null)
        {
            return null;
        }

        try
        {
            var result = method.Invoke(target, arguments);
            return result is bool boolResult ? boolResult : null;
        }
        catch
        {
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // Instance members — default IPluginHostContext implementation (preview mode)
    // -----------------------------------------------------------------------

    private bool HasLegacyHostRuntime =>
        ResolveType(HostUdtPluginSettingsWindowTypeName, HostUdtWpfAssemblyName) is not null &&
        ResolveCurrentApplication(ResolveType(WpfApplicationTypeName, "PresentationFramework")) is not null;

    /// <inheritdoc />
    public PluginHostMode Mode => HasLegacyHostRuntime ? PluginHostMode.RealRuntime : PluginHostMode.Preview;

    /// <inheritdoc />
    public bool AllowSystemActions => HasLegacyHostRuntime;

    /// <inheritdoc />
    public object? OwnerWindow
    {
        get
        {
            var windowType = ResolveType(WpfWindowTypeName, "PresentationFramework");
            return windowType is null ? null : ResolveOwnerWindow(windowType);
        }
    }

    /// <inheritdoc />
    public bool OpenPluginSettings(string pluginId)
    {
        if (!HasLegacyHostRuntime || string.IsNullOrWhiteSpace(pluginId))
        {
            return false;
        }

        var dialog = CreateHostWindow(HostUdtPluginSettingsWindowTypeName, pluginId)
                     ?? CreateHostWindow(HostPluginSettingsWindowTypeName, pluginId);
        return dialog is not null && (TryShowDialog(dialog) ?? false);
    }

    /// <inheritdoc />
    public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null)
    {
        if (!HasLegacyHostRuntime || dialogOrContent is null)
        {
            return false;
        }

        return TryShowDialog(dialogOrContent);
    }

    // -----------------------------------------------------------------------
    // Nested types
    // -----------------------------------------------------------------------

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
            {
                return;
            }

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

        public bool OpenPluginSettings(string pluginId) => TryInvokeBoolMethod(_hostContext, "OpenPluginSettings", pluginId) ?? false;

        public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null) =>
            TryInvokeBoolMethod(_hostContext, "ShowDialog", dialogOrContent, title, icon);
    }
}
