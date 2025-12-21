using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services.Settings;

/// <summary>
/// Dock launcher settings manager with extended options
/// </summary>
public class DockSettings
{
    private static readonly string SettingsFilePath = Path.Combine(
        Folders.AppData,
        "DockLauncher",
        "settings.json");

    private SettingsData _data = new();
    private readonly object _saveLock = new();
    private CancellationTokenSource? _saveCancellationTokenSource;
    private bool _isLoading;

    public bool IsEnabled
    {
        get => _data.IsEnabled;
        set
        {
            _data.IsEnabled = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public int IconSize
    {
        get => _data.IconSize;
        set
        {
            _data.IconSize = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool AutoHide
    {
        get => _data.AutoHide;
        set
        {
            _data.AutoHide = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public int AutoHideDelay
    {
        get => _data.AutoHideDelay;
        set
        {
            _data.AutoHideDelay = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public DockPosition Position
    {
        get => _data.Position;
        set
        {
            _data.Position = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public double Opacity
    {
        get => _data.Opacity;
        set
        {
            _data.Opacity = Math.Max(0.1, Math.Min(1.0, value));
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool BlurEffect
    {
        get => _data.BlurEffect;
        set
        {
            _data.BlurEffect = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool ShowRunningAppsOnly
    {
        get => _data.ShowRunningAppsOnly;
        set
        {
            _data.ShowRunningAppsOnly = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool AutoAddTaskbarApps
    {
        get => _data.AutoAddTaskbarApps;
        set
        {
            _data.AutoAddTaskbarApps = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool MinimizeToDock
    {
        get => _data.MinimizeToDock;
        set
        {
            _data.MinimizeToDock = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool ShowWindowPreview
    {
        get => _data.ShowWindowPreview;
        set
        {
            _data.ShowWindowPreview = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool EnableAnimations
    {
        get => _data.EnableAnimations;
        set
        {
            _data.EnableAnimations = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool Enable3DPerspective
    {
        get => _data.Enable3DPerspective;
        set
        {
            _data.Enable3DPerspective = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public int DockHeight
    {
        get => _data.DockHeight;
        set
        {
            _data.DockHeight = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public int DockPadding
    {
        get => _data.DockPadding;
        set
        {
            _data.DockPadding = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public List<DockItem> DockItems
    {
        get => _data.DockItems;
        set
        {
            _data.DockItems = value ?? new List<DockItem>();
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    private async Task SaveAsyncDelayed()
    {
        lock (_saveLock)
        {
            _saveCancellationTokenSource?.Cancel();
            _saveCancellationTokenSource = new CancellationTokenSource();
        }

        try
        {
            await Task.Delay(500, _saveCancellationTokenSource.Token);
            
            if (!_saveCancellationTokenSource.Token.IsCancellationRequested)
            {
                await SaveAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when a new save is triggered
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in delayed save: {ex.Message}", ex);
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            _isLoading = true;
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                _data = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
            _data = new SettingsData();
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error saving settings: {ex.Message}", ex);
        }
    }

    private class SettingsData
    {
        public bool IsEnabled { get; set; } = true;
        public int IconSize { get; set; } = 48;
        public bool AutoHide { get; set; } = true;
        public int AutoHideDelay { get; set; } = 2;
        public DockPosition Position { get; set; } = DockPosition.Bottom;
        public double Opacity { get; set; } = 0.95;
        public bool BlurEffect { get; set; } = true;
        public bool ShowRunningAppsOnly { get; set; } = false;
        public bool AutoAddTaskbarApps { get; set; } = true;
        public bool MinimizeToDock { get; set; } = true;
        public bool ShowWindowPreview { get; set; } = true;
        public bool EnableAnimations { get; set; } = true;
        public bool Enable3DPerspective { get; set; } = true;
        public int DockHeight { get; set; } = 80;
        public int DockPadding { get; set; } = 12;
        public List<DockItem> DockItems { get; set; } = new();
    }
}

/// <summary>
/// Dock position on screen
/// </summary>
public enum DockPosition
{
    Bottom,
    Top,
    Left,
    Right
}

/// <summary>
/// Dock item representing an application
/// </summary>
public class DockItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public int Order { get; set; }
    public bool IsAutoAdded { get; set; } = false; // True if auto-added from taskbar
    public bool IsTrashCan { get; set; } = false; // True if this is the trash can item
}


            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public int DockPadding
    {
        get => _data.DockPadding;
        set
        {
            _data.DockPadding = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public List<DockItem> DockItems
    {
        get => _data.DockItems;
        set
        {
            _data.DockItems = value ?? new List<DockItem>();
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    private async Task SaveAsyncDelayed()
    {
        lock (_saveLock)
        {
            _saveCancellationTokenSource?.Cancel();
            _saveCancellationTokenSource = new CancellationTokenSource();
        }

        try
        {
            await Task.Delay(500, _saveCancellationTokenSource.Token);
            
            if (!_saveCancellationTokenSource.Token.IsCancellationRequested)
            {
                await SaveAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when a new save is triggered
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in delayed save: {ex.Message}", ex);
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            _isLoading = true;
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                _data = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
            _data = new SettingsData();
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error saving settings: {ex.Message}", ex);
        }
    }

    private class SettingsData
    {
        public bool IsEnabled { get; set; } = true;
        public int IconSize { get; set; } = 48;
        public bool AutoHide { get; set; } = true;
        public int AutoHideDelay { get; set; } = 2;
        public DockPosition Position { get; set; } = DockPosition.Bottom;
        public double Opacity { get; set; } = 0.95;
        public bool BlurEffect { get; set; } = true;
        public bool ShowRunningAppsOnly { get; set; } = false;
        public bool AutoAddTaskbarApps { get; set; } = true;
        public bool MinimizeToDock { get; set; } = true;
        public bool ShowWindowPreview { get; set; } = true;
        public bool EnableAnimations { get; set; } = true;
        public bool Enable3DPerspective { get; set; } = true;
        public int DockHeight { get; set; } = 80;
        public int DockPadding { get; set; } = 12;
        public List<DockItem> DockItems { get; set; } = new();
    }
}

/// <summary>
/// Dock position on screen
/// </summary>
public enum DockPosition
{
    Bottom,
    Top,
    Left,
    Right
}

/// <summary>
/// Dock item representing an application
/// </summary>
public class DockItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public int Order { get; set; }
    public bool IsAutoAdded { get; set; } = false; // True if auto-added from taskbar
    public bool IsTrashCan { get; set; } = false; // True if this is the trash can item
}

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services.Settings;

/// <summary>
/// Dock launcher settings manager
/// </summary>
public class DockSettings
{
    private static readonly string SettingsFilePath = Path.Combine(
        Folders.AppData,
        "DockLauncher",
        "settings.json");

    private SettingsData _data = new();
    private readonly object _saveLock = new();
    private CancellationTokenSource? _saveCancellationTokenSource;
    private bool _isLoading;

    public bool IsEnabled
    {
        get => _data.IsEnabled;
        set
        {
            _data.IsEnabled = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public int IconSize
    {
        get => _data.IconSize;
        set
        {
            _data.IconSize = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool AutoHide
    {
        get => _data.AutoHide;
        set
        {
            _data.AutoHide = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public int AutoHideDelay
    {
        get => _data.AutoHideDelay;
        set
        {
            _data.AutoHideDelay = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public List<DockItem> DockItems
    {
        get => _data.DockItems;
        set
        {
            _data.DockItems = value ?? new List<DockItem>();
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    private async Task SaveAsyncDelayed()
    {
        lock (_saveLock)
        {
            _saveCancellationTokenSource?.Cancel();
            _saveCancellationTokenSource = new CancellationTokenSource();
        }

        try
        {
            await Task.Delay(500, _saveCancellationTokenSource.Token);
            
            if (!_saveCancellationTokenSource.Token.IsCancellationRequested)
            {
                await SaveAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when a new save is triggered
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in delayed save: {ex.Message}", ex);
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            _isLoading = true;
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                _data = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
            _data = new SettingsData();
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error saving settings: {ex.Message}", ex);
        }
    }

    private class SettingsData
    {
        public bool IsEnabled { get; set; } = false;
        public int IconSize { get; set; } = 48;
        public bool AutoHide { get; set; } = true;
        public int AutoHideDelay { get; set; } = 2;
        public List<DockItem> DockItems { get; set; } = new();
    }
}

/// <summary>
/// Dock item representing an application
/// </summary>
public class DockItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public int Order { get; set; }
}

