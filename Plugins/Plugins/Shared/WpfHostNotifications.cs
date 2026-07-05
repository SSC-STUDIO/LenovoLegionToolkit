using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.Shared;

/// <summary>
/// Provides host-agnostic access to WPF notification helpers (Snackbar, MessageBox, Localization)
/// by resolving the corresponding types from the host application at runtime.
/// Falls back to basic <see cref="System.Windows.MessageBox"/> when the host helpers are unavailable.
/// </summary>
internal static class WpfHostNotifications
{
    private enum SnackbarType
    {
        Info = 0,
        Error = 2,
    }

    private static class ReflectionCache
    {
        internal static readonly object? SnackbarHelperType;
        internal static readonly object? MessageBoxHelperType;
        internal static readonly object? LocalizationHelperType;
        internal static readonly object? SnackbarTypeType;

        static ReflectionCache()
        {
            var hostAssembly = ResolveHostWpfAssembly();
            if (hostAssembly is null)
                return;

            SnackbarHelperType = hostAssembly.GetType("LenovoLegionToolkit.WPF.Utils.SnackbarHelper", throwOnError: false, ignoreCase: false);
            MessageBoxHelperType = hostAssembly.GetType("LenovoLegionToolkit.WPF.Utils.MessageBoxHelper", throwOnError: false, ignoreCase: false);
            LocalizationHelperType = hostAssembly.GetType("LenovoLegionToolkit.WPF.Utils.LocalizationHelper", throwOnError: false, ignoreCase: false);
            SnackbarTypeType = hostAssembly.GetType("LenovoLegionToolkit.WPF.Utils.SnackbarType", throwOnError: false, ignoreCase: false);
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
                try
                {
                    return Assembly.Load(new AssemblyName(name));
                }
                catch
                {
                    // Try next
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Shows a snackbar notification with the specified title and message.
    /// Falls back to <see cref="System.Windows.MessageBox"/> when the host helper is unavailable.
    /// </summary>
    internal static void ShowSnackbar(string title, string message, bool isError = false)
    {
        if (ReflectionCache.SnackbarHelperType is not null)
        {
            try
            {
                var showMethod = ReflectionCache.SnackbarHelperType.GetMethod("Show", [typeof(string), typeof(string)]);
                if (showMethod is not null)
                {
                    showMethod.Invoke(null, [title, message]);
                    return;
                }
            }
            catch
            {
                // Fall through to fallback
            }

            if (isError && ReflectionCache.SnackbarTypeType is not null)
            {
                try
                {
                    var errorValue = Enum.Parse(ReflectionCache.SnackbarTypeType as Type ?? throw new InvalidOperationException(), "Error");
                    var showMethod = ReflectionCache.SnackbarHelperType.GetMethod("Show", [typeof(string), typeof(string), ReflectionCache.SnackbarTypeType as Type ?? typeof(int)]);
                    if (showMethod is not null)
                    {
                        showMethod.Invoke(null, [title, message, errorValue]);
                        return;
                    }
                }
                catch
                {
                    // Fall through to fallback
                }
            }
        }

        // Fallback: use MessageBox for errors, debug trace for info
        if (isError)
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        else
            PluginLog.Trace($"[Snackbar] {title}: {message}");
    }

    /// <summary>
    /// Shows an error snackbar notification.
    /// </summary>
    internal static void ShowSnackbarError(string title, string message)
    {
        ShowSnackbar(title, message, isError: true);
    }

    /// <summary>
    /// Shows a confirmation dialog with primary and secondary buttons.
    /// Returns true if the primary button is clicked, false for secondary/cancel.
    /// </summary>
    internal static async Task<bool> ShowConfirmAsync(Window? owner, string title, string message, string primaryText, string secondaryText)
    {
        if (ReflectionCache.MessageBoxHelperType is not null)
        {
            try
            {
                var showMethod = ReflectionCache.MessageBoxHelperType.GetMethod("ShowAsync", [
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
        if (ReflectionCache.MessageBoxHelperType is not null)
        {
            try
            {
                var showMethod = ReflectionCache.MessageBoxHelperType.GetMethod("ShowInputAsync", [
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

        // Fallback: use InputBox or simple MessageBox
        PluginLog.Trace($"[InputDialog] {title}: {placeholder} (fallback - input dialog not available)");
        return null;
    }

    /// <summary>
    /// Applies plugin resource cultures for proper localization.
    /// No-op if the host helper is unavailable.
    /// </summary>
    internal static void SetPluginResourceCultures()
    {
        if (ReflectionCache.LocalizationHelperType is null)
            return;

        try
        {
            var method = ReflectionCache.LocalizationHelperType.GetMethod("SetPluginResourceCultures", Type.EmptyTypes);
            method?.Invoke(null, null);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error applying plugin resource cultures: {ex.Message}", ex);
        }
    }
}
