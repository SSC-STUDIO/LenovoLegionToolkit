using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin update manager - handles automatic and manual update checks
/// </summary>
public class PluginUpdateManager : IDisposable
{
    private readonly PluginRepositoryService _repositoryService;
    private readonly VersionChecker _versionChecker;
    private readonly ApplicationSettings _settings;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private CancellationTokenSource? _backgroundCheckCts;
    private bool _disposed;

    /// <summary>
    /// Event fired when updates are found
    /// </summary>
    public event EventHandler<List<PluginUpdateInfo>>? UpdatesFound;

    /// <summary>
    /// Event fired when an error occurs during update check
    /// </summary>
    public event EventHandler<string>? UpdateCheckFailed;

    /// <summary>
    /// Event fired when update check completes
    /// </summary>
    public event EventHandler? UpdateCheckCompleted;

    /// <summary>
    /// Creates a new PluginUpdateManager
    /// </summary>
    public PluginUpdateManager(PluginRepositoryService repositoryService, ApplicationSettings settings)
    {
        _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _versionChecker = new VersionChecker();
    }

    /// <summary>
    /// Creates a new PluginUpdateManager with the current host version
    /// </summary>
    public PluginUpdateManager(PluginRepositoryService repositoryService, ApplicationSettings settings, string currentHostVersion)
    {
        _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _versionChecker = new VersionChecker(currentHostVersion);
    }

    /// <summary>
    /// Check if update check is needed based on settings
    /// </summary>
    public bool ShouldCheckForUpdates()
    {
        var store = _settings.Store;
        
        if (!store.CheckPluginUpdatesOnStartup)
            return false;

        var lastCheck = store.LastPluginUpdateCheckTime;
        if (lastCheck == null)
            return true;

        var hoursSinceLastCheck = (DateTime.Now - lastCheck.Value).TotalHours;
        return hoursSinceLastCheck >= store.PluginUpdateCheckFrequencyHours;
    }

    /// <summary>
    /// Check for updates on application startup
    /// </summary>
    public async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"PluginUpdateManager: Checking for updates on startup...");

            var result = await CheckForUpdatesAsync();

            if (result.HasUpdates)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"PluginUpdateManager: Found {result.AvailableUpdates.Count} updates on startup");

                UpdatesFound?.Invoke(this, result.AvailableUpdates);
            }
            else
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"PluginUpdateManager: No updates found on startup");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"PluginUpdateManager: Update check failed on startup: {ex.Message}");
        }
    }

    /// <summary>
    /// Perform a manual update check
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"PluginUpdateManager: Starting manual update check...");

            var store = _settings.Store;
            var installedPlugins = GetInstalledPluginVersions();

            try
            {
                var availablePlugins = await _repositoryService.FetchAvailablePluginsAsync();

                var updates = _versionChecker.GetAvailableUpdates(installedPlugins, availablePlugins);

                var incompatible = _versionChecker.CheckCompatibility(availablePlugins);

                store.LastPluginUpdateCheckTime = DateTime.Now;
                _settings.SynchronizeStore();

                var result = new CompatibilityUpdateCheckResult
                {
                    IsSuccess = true,
                    LastCheckTime = store.LastPluginUpdateCheckTime,
                    AvailableUpdates = updates,
                    IncompatiblePlugins = incompatible
                };

                if (updates.Count > 0)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"PluginUpdateManager: Found {updates.Count} updates");
                    UpdatesFound?.Invoke(this, updates);
                }

                UpdateCheckCompleted?.Invoke(this, EventArgs.Empty);
                return result;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"PluginUpdateManager: Update check failed: {ex.Message}");

                UpdateCheckFailed?.Invoke(this, ex.Message);
                UpdateCheckCompleted?.Invoke(this, EventArgs.Empty);

                return new UpdateCheckResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    LastCheckTime = store.LastPluginUpdateCheckTime
                };
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Start background update checking
    /// </summary>
    public void StartBackgroundCheck()
    {
        if (_backgroundCheckCts != null)
            return;

        _backgroundCheckCts = new CancellationTokenSource();
        
        _ = Task.Run(async () =>
        {
            while (!_backgroundCheckCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), _backgroundCheckCts.Token);
                    
                    if (ShouldCheckForUpdates())
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"PluginUpdateManager: Running background update check...");
                        
                        var result = await CheckForUpdatesAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"PluginUpdateManager: Background check failed: {ex.Message}");
                }
            }
        });
    }

    /// <summary>
    /// Stop background update checking
    /// </summary>
    public void StopBackgroundCheck()
    {
        _backgroundCheckCts?.Cancel();
        _backgroundCheckCts?.Dispose();
        _backgroundCheckCts = null;
    }

    /// <summary>
    /// Download and install a specific update
    /// </summary>
    public async Task<bool> InstallUpdateAsync(PluginUpdateInfo update, IProgress<double>? progress = null)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"PluginUpdateManager: Installing update for {update.PluginId}...");

            var manifest = new PluginManifest
            {
                Id = update.PluginId,
                Version = update.NewVersion,
                DownloadUrl = update.DownloadUrl,
                Changelog = update.Changelog
            };

            var success = await _repositoryService.DownloadAndInstallPluginAsync(manifest);

            if (success)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"PluginUpdateManager: Successfully installed update for {update.PluginId}");

                MarkUpdatePending(update.PluginId, false);
            }

            return success;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"PluginUpdateManager: Failed to install update for {update.PluginId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get installed plugin versions
    /// </summary>
    private Dictionary<string, string> GetInstalledPluginVersions()
    {
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        var pluginsDir = GetPluginsDirectory();
        if (!Directory.Exists(pluginsDir))
            return versions;

        var subdirs = Directory.GetDirectories(pluginsDir);
        foreach (var subdir in subdirs)
        {
            var pluginId = Path.GetFileName(subdir);
            
            var pluginInfo = Path.Combine(subdir, "plugin.json");
            if (File.Exists(pluginInfo))
            {
                try
                {
                    var json = File.ReadAllText(pluginInfo);
                    var manifest = global::System.Text.Json.JsonSerializer.Deserialize<PluginManifest>(json, new global::System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (manifest != null && !string.IsNullOrEmpty(manifest.Version))
                    {
                        versions[manifest.Id] = manifest.Version;
                    }
                }
                catch
                {
                }
            }
        }

        return versions;
    }

    /// <summary>
    /// Get plugins directory
    /// </summary>
    private string GetPluginsDirectory()
    {
        var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(appBaseDir, "build", "plugins"),
            Path.Combine(appBaseDir, "..", "..", "..", "build", "plugins"),
            Path.Combine(appBaseDir, "..", "build", "plugins"),
            Path.Combine(appBaseDir, "Plugins"),
            Path.Combine(appBaseDir, "plugins"),
        };

        foreach (var possiblePath in possiblePaths)
        {
            var fullPath = Path.GetFullPath(possiblePath);
            if (Directory.Exists(fullPath))
                return fullPath;
        }

        var defaultPath = Path.Combine(appBaseDir, "build", "plugins");
        Directory.CreateDirectory(defaultPath);
        return defaultPath;
    }

    /// <summary>
    /// Mark an update as pending
    /// </summary>
    private void MarkUpdatePending(string pluginId, bool pending)
    {
        var store = _settings.Store;
        
        if (pending)
        {
            if (!store.PendingPluginUpdates.Contains(pluginId))
                store.PendingPluginUpdates.Add(pluginId);
        }
        else
        {
            store.PendingPluginUpdates.Remove(pluginId);
        }
        
        _settings.SynchronizeStore();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopBackgroundCheck();
        _semaphore.Dispose();
        _disposed = true;
    }
}
