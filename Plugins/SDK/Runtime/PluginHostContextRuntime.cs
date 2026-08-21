using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
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
    private const string HostPluginHostContextTypeName = "UniversalDeviceToolkit.Lib.Plugins.PluginHostContext";

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
            }

            IPluginHostContext? bridge = null;
            try
            {
                bridge = TryResolveHostBridge();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SDK] Host bridge resolution failed: {ex.GetType().Name}: {ex.Message}");
            }

            lock (_contextLock)
            {
                if (!ReferenceEquals(_current, DefaultContext))
                {
                    return _current;
                }

                if (bridge is not null)
                {
                    _current = bridge;
                    return bridge;
                }

                return DefaultContext;
            }
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            lock (_contextLock)
            {
                _current = value;
            }
        }
    }

    /// <summary>
    /// Pushes a temporary context and returns a scope that restores the previous
    /// context on dispose.
    /// </summary>
    public static IDisposable Push(IPluginHostContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IPluginHostContext previous;
        lock (_contextLock)
        {
            previous = _current;
            _current = context;
        }

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
    /// Creates an instance of a host window type by full type name.
    /// The WPF host is gone; this always returns null.
    /// </summary>
    public static object? CreateHostWindow(string fullTypeName, params object[] constructorArguments)
    {
        _ = fullTypeName;
        _ = constructorArguments;
        return null;
    }

    private static IPluginHostContext? TryResolveHostBridge()
    {
        var hostContextType = ResolveType(HostPluginHostContextTypeName, HostUdtLibAssemblyName)
                              ?? ResolveType(HostPluginHostContextTypeName, HostLibAssemblyName);
        if (hostContextType is null)
        {
            return null;
        }

        var currentProperty = GetCachedProperty(hostContextType, "Current", BindingFlags.Public | BindingFlags.Static);
        if (currentProperty is null)
        {
            return null;
        }

        var bridgedContext = new BridgedHostContext(currentProperty);
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
            catch (Exception ex) when (ex is TypeLoadException or FileLoadException or FileNotFoundException or ArgumentException or BadImageFormatException)
            {
                return null;
            }
        }

        try
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            return assembly.GetType(fullTypeName, throwOnError: false, ignoreCase: false);
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SDK] ResolveType(\"{fullTypeName}\", \"{assemblyName}\") failed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static PropertyInfo? GetCachedProperty(Type type, string name, BindingFlags bindingFlags)
    {
        return _propertyCache.GetOrAdd((type, name), _ =>
        {
            try
            {
                return type.GetProperty(name, bindingFlags);
            }
            catch (AmbiguousMatchException)
            {
                return null;
            }
        });
    }

    private static MethodInfo? GetCachedMethod(Type type, string name, BindingFlags bindingFlags)
    {
        return _methodCache.GetOrAdd((type, name), _ =>
        {
            try
            {
                return type.GetMethod(name, bindingFlags);
            }
            catch (AmbiguousMatchException)
            {
                return null;
            }
        });
    }

    private static object? TryReadProperty(object target, string propertyName)
    {
        try
        {
            var property = GetCachedProperty(target.GetType(), propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(target);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SDK] TryReadProperty(\"{propertyName}\") failed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Invokes a method by name via reflection and interprets the result as a
    /// nullable <see cref="bool"/>.  Returns <c>null</c> when the method is missing
    /// or the return value is not a boolean.
    /// </summary>
    private static bool? TryInvokeBoolMethod(object target, string methodName, params object?[] arguments)
    {
        try
        {
            var method = GetCachedMethod(target.GetType(), methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method is null)
            {
                return null;
            }

            var result = method.Invoke(target, arguments);
            return result is bool boolResult ? boolResult : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SDK] TryInvokeBoolMethod(\"{methodName}\") failed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // Instance members - default IPluginHostContext implementation (preview mode)
    // -----------------------------------------------------------------------

    /// <inheritdoc />
    public PluginHostMode Mode => PluginHostMode.Preview;

    /// <inheritdoc />
    public bool AllowSystemActions => false;

    /// <inheritdoc />
    public object? OwnerWindow => null;

    /// <inheritdoc />
    public bool OpenPluginSettings(string pluginId)
    {
        _ = pluginId;
        return false;
    }

    /// <inheritdoc />
    public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null)
    {
        _ = dialogOrContent;
        _ = title;
        _ = icon;
        return null;
    }

    // -----------------------------------------------------------------------
    // Nested types
    // -----------------------------------------------------------------------

    private sealed class RestoreScope : IDisposable
    {
        private readonly IPluginHostContext _previous;
        private int _disposed;

        public RestoreScope(IPluginHostContext previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            lock (_contextLock)
            {
                _current = _previous;
            }
        }
    }

    private sealed class BridgedHostContext : IPluginHostContext
    {
        private readonly PropertyInfo _currentProperty;

        public BridgedHostContext(PropertyInfo currentProperty)
        {
            _currentProperty = currentProperty;
        }

        public bool IsActive
        {
            get
            {
                var hostContext = LiveHostContext();
                if (hostContext is null)
                {
                    return false;
                }

                return ReadAllowSystemActions(hostContext)
                    || ReadOwnerWindow(hostContext) is not null
                    || ReadMode(hostContext) != PluginHostMode.Preview;
            }
        }

        public PluginHostMode Mode
        {
            get
            {
                var hostContext = LiveHostContext();
                return hostContext is null ? PluginHostMode.Preview : ReadMode(hostContext);
            }
        }

        public bool AllowSystemActions
        {
            get
            {
                var hostContext = LiveHostContext();
                return hostContext is not null && ReadAllowSystemActions(hostContext);
            }
        }

        public object? OwnerWindow
        {
            get
            {
                var hostContext = LiveHostContext();
                return hostContext is null ? null : ReadOwnerWindow(hostContext);
            }
        }

        public bool OpenPluginSettings(string pluginId)
        {
            var hostContext = LiveHostContext();
            return hostContext is not null && (TryInvokeBoolMethod(hostContext, "OpenPluginSettings", pluginId) ?? false);
        }

        public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null)
        {
            var hostContext = LiveHostContext();
            return hostContext is null
                ? null
                : TryInvokeBoolMethod(hostContext, "ShowDialog", dialogOrContent, title, icon);
        }

        private object? LiveHostContext()
        {
            try
            {
                return _currentProperty.GetValue(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SDK] Host PluginHostContext.Current read failed: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static PluginHostMode ReadMode(object hostContext)
        {
            try
            {
                var modeValue = TryReadProperty(hostContext, "Mode");
                if (modeValue is PluginHostMode pluginHostMode)
                {
                    return pluginHostMode;
                }

                if (modeValue is int intMode)
                {
                    return intMode == (int)PluginHostMode.RealRuntime
                        ? PluginHostMode.RealRuntime
                        : PluginHostMode.Preview;
                }

                return string.Equals(modeValue?.ToString(), "RealRuntime", StringComparison.OrdinalIgnoreCase)
                    ? PluginHostMode.RealRuntime
                    : PluginHostMode.Preview;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SDK] Host Mode read failed: {ex.GetType().Name}: {ex.Message}");
                return PluginHostMode.Preview;
            }
        }

        private static bool ReadAllowSystemActions(object hostContext) =>
            TryReadProperty(hostContext, "AllowSystemActions") is bool allowSystemActions && allowSystemActions;

        private static object? ReadOwnerWindow(object hostContext) => TryReadProperty(hostContext, "OwnerWindow");
    }
}
