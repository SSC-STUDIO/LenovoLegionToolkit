using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class PluginExtensionsPage
{
    private async void BulkImportButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var zipFilePaths = await ResolveBulkImportZipFilePathsAsync();
            if (zipFilePaths.Count > 0)
            {
                SnackbarHelper.Show(Resource.PluginExtensionsPage_ImportProgress, Resource.PluginExtensionsPage_ImportProgress, SnackbarType.Info);

                int importedCount = 0;
                foreach (var zipFilePath in zipFilePaths)
                {
                    try
                    {
                        // Extract and install plugin
                        var result = await ExtractAndInstallPluginAsync(zipFilePath);
                        if (result)
                        {
                            importedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error importing plugin from {zipFilePath}: {ex.Message}", ex);

                        SnackbarHelper.Show(Resource.PluginExtensionsPage_BulkImportFailed,
                            string.Format(Resource.PluginExtensionsPage_BulkImportFailedMessage, Path.GetFileName(zipFilePath), ex.Message), SnackbarType.Error);
                    }
                }

                // Refresh plugins and UI
                await _pluginManager.ScanAndLoadPluginsAsync();
                LocalizationHelper.SetPluginResourceCultures();
                UpdateAllPluginsUI();

                // Show success message
                if (importedCount > 0)
                {
                    SnackbarHelper.Show(
                        string.Format(Resource.Culture ?? CultureInfo.CurrentUICulture, Resource.PluginExtensionsPage_BulkImportSuccess, importedCount),
                        string.Format(Resource.Culture ?? CultureInfo.CurrentUICulture, Resource.PluginExtensionsPage_BulkImportSuccessMessage, importedCount),
                        SnackbarType.Success);
                }
            }
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error in bulk import: {ex.Message}", ex);

            SnackbarHelper.Show(Resource.PluginExtensionsPage_BulkImportFailed,
                string.Format(
                    Resource.PluginExtensionsPage_BulkImportFailedMessage,
                    T("PluginExtensionsPage_UnknownSource", "Unknown"),
                    ex.Message),
                SnackbarType.Error);
        }
    }

    private async Task<bool> ExtractAndInstallPluginAsync(string zipFilePath)
    {
        var pluginsDir = GetPluginsDirectory();
        try
        {
            var installationService = new PluginInstallationService(_pluginManager);
            return await installationService.ExtractAndInstallPluginAsync(zipFilePath, pluginsDir);
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error installing plugin from {zipFilePath}: {ex.Message}", ex);
            return false;
        }
    }

    private async Task<IReadOnlyList<string>> ResolveBulkImportZipFilePathsAsync()
    {
        return await PromptForBulkImportZipFilePathsAsync();
    }

    // AVALONIA: WPF Microsoft.Win32.OpenFileDialog replaced by the Avalonia file picker
    // (TopLevel.StorageProvider). Filter string ("ZIP Files (*.zip)|...") becomes a
    // FilePickerFileType { Patterns } collection + All fallback.
    private async Task<IReadOnlyList<string>> PromptForBulkImportZipFilePathsAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is { } storage)
        {
            var picked = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Resource.PluginExtensionsPage_SelectPluginFiles,
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(T("PluginExtensionsPage_ZipFileFilter", "ZIP Files"))
                    {
                        Patterns = new[] { "*.zip" }
                    },
                    FilePickerFileTypes.All
                }
            });

            return picked
                .Select(file => file.TryGetLocalPath() ?? file.Path.LocalPath)
                .ToArray();
        }

        return Array.Empty<string>();
    }

    private string GetPluginsDirectory()
    {
        var pluginsDirectory = PluginPaths.GetPluginsDirectory();
        if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Using plugins directory: {pluginsDirectory}");
        return pluginsDirectory;
    }

    private List<PluginManifest> BuildInstalledPluginManifestsForUpdateCheck()
    {
        var manifests = new List<PluginManifest>();

        foreach (var pluginId in _pluginManager.GetInstalledPluginIds().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _pluginManager.TryGetPlugin(pluginId, out var plugin);
            var metadata = _pluginManager.GetPluginMetadata(pluginId);

            manifests.Add(new PluginManifest
            {
                Id = pluginId,
                Name = plugin?.Name ?? metadata?.Name ?? pluginId,
                Description = plugin?.Description ?? metadata?.Description ?? string.Empty,
                Version = ResolveInstalledPluginVersion(pluginId) ?? metadata?.Version ?? "0.0.0",
                Icon = plugin?.Icon ?? metadata?.Icon ?? string.Empty,
                IsSystemPlugin = plugin?.IsSystemPlugin ?? metadata?.IsSystemPlugin ?? false
            });
        }

        return manifests;
    }

    /// <summary>List/badge path: no Authenticode (UI-thread safe).</summary>
    private bool TryResolvePluginExecutableForListing(string pluginId)
    {
        var metadata = _pluginManager.GetPluginMetadata(pluginId);
        return PluginExecutableResolver.TryResolveForUiListing(
            pluginId,
            metadata?.FilePath,
            GetPluginsDirectory(),
            out _,
            out _);
    }

    /// <summary>Launch path: Authenticode required outside DEBUG.</summary>
    private bool TryResolvePluginExecutable(string pluginId, out string? exeFile, out string? workingDirectory)
    {
        var metadata = _pluginManager.GetPluginMetadata(pluginId);
#if DEBUG
        return PluginExecutableResolver.TryResolve(
            pluginId,
            metadata?.FilePath,
            GetPluginsDirectory(),
            out exeFile,
            out workingDirectory,
            allowUnsignedOverride: true,
            verifyAuthenticode: true);
#else
        return PluginExecutableResolver.TryResolve(
            pluginId,
            metadata?.FilePath,
            GetPluginsDirectory(),
            out exeFile,
            out workingDirectory,
            allowUnsignedOverride: false,
            verifyAuthenticode: true);
#endif
    }
}
