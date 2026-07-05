using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.Shared;

/// <summary>
/// Provides host-agnostic access to WPF notification helpers (Snackbar, MessageBox, Localization)
/// by resolving the corresponding types from the host application at runtime via reflection.
/// Falls back to basic <see cref="System.Windows.MessageBox"/> when the host helpers are unavailable.
/// This eliminates the hard compile-time dependency on LenovoLegionToolkit.WPF assembly.
/// </summary>
internal static class WpfHostNotifications
{
    private static class ReflectionCache
    {
        internal static readonly Type? SnackbarHelperType;
        internal static readonly Type? MessageBoxHelperType;
        internal static readonly Type? LocalizationHelperType;
        internal static readonly Type? SnackbarTypeType;
        internal static readonly object? SnackbarTypeErrorValue;

        static ReflectionCache()
        {
            var hostAssembly = ResolveHostWpfAssembly();
            if (hostAssembly is null)
                return;

            SnackbarHelperType = hostAssembly.GetType("LenovoLegionToolkit.WPF.Utils.SnackbarHelper", throwOnError: false, ignoreCase: false);
            MessageBoxHelperType = hostAssembly.GetType("LenovoLegionToolkit.WPF.Utils.MessageBoxHelper", throwOnError: false, ignoreCase: false);
            LocalizationHelperType = hostAssembly.GetType("LenovoLegionToolkit.WPF.Utils.LocalizationHelper", throwOnError: false, ignoreCase: false);
            SnackbarTypeType = hostAssembly.GetType("LenovoLegionToolkit.WPF.Utils.SnackbarType", throwOnError: false, ignoreCase: false);

            if (SnackbarTypeType is not null)
            {
                try { SnackbarTypeErrorValue = Enum.Parse(SnackbarTypeType, "Error"); }
                catch { /* ignore */ }
            }
        }

        private static Assembly? ResolveHostWpfAssembly()
        {
            var wpfAssemblyNames = new[]
            {
                "Lenovo Legion Toolkit",
                "Universal Device Toolkit",
            };

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var name in wpfAssemblyNames)
                {
                    if (string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                        return assembly;
                }
            }

            foreach (var name in wpfAssemblyNames)
            {
                try { return Assembly.Load(new AssemblyName(name)); }
                catch { /* try next */ }
            }

            return null;
        }
    }

    /// <summary>
    /// Shows a snackbar notification with the specified title and message.
    /// Falls back to System.Windows.MessageBox for errors, or PluginLog.Trace for info.
    /// </summary>
    internal static void ShowSnackbar(string title, string message)
    {
        if (TrySnackbarShow(title, message, null))
            return;

        PluginLog.Trace($"[Snackbar] {title}: {message}");
    }

    /// <summary>
    /// Shows an error snackbar notification.
    /// </summary>
    internal static void ShowSnackbarError(string title, string message)
    {
        if (TrySnackbarShow(title, message, ReflectionCache.SnackbarTypeErrorValue))
            return;

        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static bool TrySnackbarShow(string title, string message, object? snackbarTypeErrorValue)
    {
        if (ReflectionCache.SnackbarHelperType is not { } helperType)
            return false;

        try
        {
            if (snackbarTypeErrorValue is not null && ReflectionCache.SnackbarTypeType is { } snackbarType)
            {
                var showMethod = helperType.GetMethod("Show", [typeof(string), typeof(string), snackbarType]);
                if (showMethod is not null)
                {
                    showMethod.Invoke(null, [title, message, snackbarTypeErrorValue]);
                    return true;
                }
            }

            var showMethod2 = helperType.GetMethod("Show", [typeof(string), typeof(string)]);
            if (showMethod2 is not null)
            {
                showMethod2.Invoke(null, [title, message]);
                return true;
            }
        }
        catch
        {
            // Reflection call failed — caller will use fallback
        }

        return false;
    }

    /// <summary>
    /// Shows a confirmation dialog with primary and secondary buttons.
    /// Returns true if the primary button is clicked, false for secondary/cancel.
    /// </summary>
    internal static async Task<bool> ShowConfirmAsync(Window? owner, string title, string message, string primaryText, string secondaryText)
    {
        if (ReflectionCache.MessageBoxHelperType is { } helperType)
        {
            try
            {
                var showMethod = helperType.GetMethod("ShowAsync", [
                    typeof(Window), typeof(string), typeof(string), typeof(string), typeof(string)
                ]);
                if (showMethod is not null)
                {
                    var result = await ((Task<bool>?)showMethod.Invoke(null, [owner, title, message, primaryText, secondaryText]) ?? Task.FromResult(false));
                    return result;
                }
            }
            catch
            {
                // Fall through to fallback
            }
        }

        // Fallback
        var fallbackResult = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return fallbackResult == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Shows an input dialog and returns the user's input, or null if canceled.
    /// </summary>
    internal static async Task<string?> ShowInputAsync(Window? owner, string title, string placeholder, string? defaultValue, string primaryText, string secondaryText, bool isMultiline = false)
    {
        if (ReflectionCache.MessageBoxHelperType is { } helperType)
        {
            try
            {
                var showMethod = helperType.GetMethod("ShowInputAsync", [
                    typeof(Window), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool)
                ]);
                if (showMethod is not null)
                {
                    var result = await ((Task<string?>?)showMethod.Invoke(null, [owner, title, placeholder, defaultValue, primaryText, secondaryText, isMultiline]) ?? Task.FromResult<string?>(null));
                    return result;
                }
            }
            catch
            {
                // Fall through to fallback
            }
        }

        // Fallback: log and return null (input dialog not available without host helpers)
        PluginLog.Trace($"[InputDialog] {title}: {placeholder} (host input dialog not available)");
        return null;
    }

    /// <summary>
    /// Applies plugin resource cultures for proper localization.
    /// No-op if the host helper is unavailable.
    /// </summary>
    internal static void SetPluginResourceCultures()
    {
        if (ReflectionCache.LocalizationHelperType is not { } helperType)
            return;

        try
        {
            var method = helperType.GetMethod("SetPluginResourceCultures", Type.EmptyTypes);
            method?.Invoke(null, null);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error applying plugin resource cultures: {ex.Message}", ex);
        }
    }
}
