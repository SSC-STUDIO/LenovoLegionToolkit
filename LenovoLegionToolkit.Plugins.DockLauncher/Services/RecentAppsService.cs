using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services;

/// <summary>
/// Service for tracking recently used applications
/// </summary>
public class RecentAppsService
{
    private static readonly string RecentAppsFilePath = Path.Combine(
        Folders.AppData,
        "DockLauncher",
        "recentapps.json");

    private readonly Dictionary<string, RecentAppInfo> _recentApps = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private const int MaxRecentApps = 10;

    /// <summary>
    /// Record application usage
    /// </summary>
    public void RecordAppUsage(string executablePath, string appName)
    {
        if (string.IsNullOrEmpty(executablePath))
            return;

        lock (_lock)
        {
            var now = DateTime.Now;
            if (_recentApps.TryGetValue(executablePath, out var existing))
            {
                existing.LastUsed = now;
                existing.UseCount++;
            }
            else
            {
                _recentApps[executablePath] = new RecentAppInfo
                {
                    ExecutablePath = executablePath,
                    AppName = appName ?? Path.GetFileNameWithoutExtension(executablePath),
                    FirstUsed = now,
                    LastUsed = now,
                    UseCount = 1
                };
            }

            // Save asynchronously
            _ = SaveAsync();
        }
    }

    /// <summary>
    /// Get recently used applications, sorted by last used time
    /// </summary>
    public List<RecentAppInfo> GetRecentApps(int maxCount = MaxRecentApps)
    {
        lock (_lock)
        {
            return _recentApps.Values
                .OrderByDescending(a => a.LastUsed)
                .Take(maxCount)
                .ToList();
        }
    }

    /// <summary>
    /// Remove application from recent list
    /// </summary>
    public void RemoveApp(string executablePath)
    {
        lock (_lock)
        {
            _recentApps.Remove(executablePath);
            _ = SaveAsync();
        }
    }

    /// <summary>
    /// Clear all recent apps
    /// </summary>
    public void ClearRecentApps()
    {
        lock (_lock)
        {
            _recentApps.Clear();
            _ = SaveAsync();
        }
    }

    /// <summary>
    /// Load recent apps from file
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(RecentAppsFilePath))
                return;

            var json = await File.ReadAllTextAsync(RecentAppsFilePath);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var data = JsonSerializer.Deserialize<RecentAppsData>(json);
            if (data?.RecentApps != null)
            {
                lock (_lock)
                {
                    _recentApps.Clear();
                    foreach (var app in data.RecentApps)
                    {
                        _recentApps[app.ExecutablePath] = app;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading recent apps: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Save recent apps to file
    /// </summary>
    private async Task SaveAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(RecentAppsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            RecentAppsData data;
            lock (_lock)
            {
                data = new RecentAppsData
                {
                    RecentApps = _recentApps.Values.ToList()
                };
            }

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(RecentAppsFilePath, json);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error saving recent apps: {ex.Message}", ex);
        }
    }

    private class RecentAppsData
    {
        public List<RecentAppInfo> RecentApps { get; set; } = new();
    }
}

/// <summary>
/// Information about a recently used application
/// </summary>
public class RecentAppInfo
{
    public string ExecutablePath { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public DateTime FirstUsed { get; set; }
    public DateTime LastUsed { get; set; }
    public int UseCount { get; set; }
}

