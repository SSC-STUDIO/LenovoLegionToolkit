using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.DockLauncher.Services.Settings;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Dock launcher settings page with extended options
/// </summary>
public partial class DockLauncherSettingsPage : INotifyPropertyChanged
{
    private readonly DockSettings _settings;
    private int _iconSize;
    private int _dockHeight;
    private double _opacity;
    private DockPosition _position;
    private bool _autoHide;
    private int _autoHideDelay;
    private bool _autoAddTaskbarApps;
    private bool _showRunningAppsOnly;
    private bool _blurEffect;
    private bool _enableAnimations;
    private bool _showWindowPreview;

    public int IconSize
    {
        get => _iconSize;
        set
        {
            if (_iconSize == value)
                return;
            
            _iconSize = value;
            OnPropertyChanged();
            _settings.IconSize = value;
        }
    }

    public int DockHeight
    {
        get => _dockHeight;
        set
        {
            if (_dockHeight == value)
                return;
            
            _dockHeight = value;
            OnPropertyChanged();
            _settings.DockHeight = value;
        }
    }

    public new double Opacity
    {
        get => _opacity;
        set
        {
            if (Math.Abs(_opacity - value) < 0.01)
                return;
            
            _opacity = value;
            OnPropertyChanged();
            _settings.Opacity = value;
        }
    }

    public int PositionIndex
    {
        get => (int)_position;
        set
        {
            if ((int)_position == value)
                return;
            
            _position = (DockPosition)value;
            OnPropertyChanged();
            _settings.Position = _position;
        }
    }

    public bool AutoHide
    {
        get => _autoHide;
        set
        {
            if (_autoHide == value)
                return;
            
            _autoHide = value;
            OnPropertyChanged();
            _settings.AutoHide = value;
        }
    }

    public int AutoHideDelay
    {
        get => _autoHideDelay;
        set
        {
            if (_autoHideDelay == value)
                return;
            
            _autoHideDelay = value;
            OnPropertyChanged();
            _settings.AutoHideDelay = value;
        }
    }

    public bool AutoAddTaskbarApps
    {
        get => _autoAddTaskbarApps;
        set
        {
            if (_autoAddTaskbarApps == value)
                return;
            
            _autoAddTaskbarApps = value;
            OnPropertyChanged();
            _settings.AutoAddTaskbarApps = value;
        }
    }

    public bool ShowRunningAppsOnly
    {
        get => _showRunningAppsOnly;
        set
        {
            if (_showRunningAppsOnly == value)
                return;
            
            _showRunningAppsOnly = value;
            OnPropertyChanged();
            _settings.ShowRunningAppsOnly = value;
        }
    }

    public bool BlurEffect
    {
        get => _blurEffect;
        set
        {
            if (_blurEffect == value)
                return;
            
            _blurEffect = value;
            OnPropertyChanged();
            _settings.BlurEffect = value;
        }
    }

    public bool EnableAnimations
    {
        get => _enableAnimations;
        set
        {
            if (_enableAnimations == value)
                return;
            
            _enableAnimations = value;
            OnPropertyChanged();
            _settings.EnableAnimations = value;
        }
    }

    public bool ShowWindowPreview
    {
        get => _showWindowPreview;
        set
        {
            if (_showWindowPreview == value)
                return;
            
            _showWindowPreview = value;
            OnPropertyChanged();
            _settings.ShowWindowPreview = value;
        }
    }

    public DockLauncherSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
        
        _settings = new DockSettings();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            await _settings.LoadAsync();
            
            IconSize = _settings.IconSize;
            DockHeight = _settings.DockHeight;
            Opacity = _settings.Opacity;
            PositionIndex = (int)_settings.Position;
            AutoHide = _settings.AutoHide;
            AutoHideDelay = _settings.AutoHideDelay;
            AutoAddTaskbarApps = _settings.AutoAddTaskbarApps;
            ShowRunningAppsOnly = _settings.ShowRunningAppsOnly;
            BlurEffect = _settings.BlurEffect;
            EnableAnimations = _settings.EnableAnimations;
            ShowWindowPreview = _settings.ShowWindowPreview;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


        set
        {
            if (_autoHideDelay == value)
                return;
            
            _autoHideDelay = value;
            OnPropertyChanged();
            _settings.AutoHideDelay = value;
        }
    }

    public bool AutoAddTaskbarApps
    {
        get => _autoAddTaskbarApps;
        set
        {
            if (_autoAddTaskbarApps == value)
                return;
            
            _autoAddTaskbarApps = value;
            OnPropertyChanged();
            _settings.AutoAddTaskbarApps = value;
        }
    }

    public bool ShowRunningAppsOnly
    {
        get => _showRunningAppsOnly;
        set
        {
            if (_showRunningAppsOnly == value)
                return;
            
            _showRunningAppsOnly = value;
            OnPropertyChanged();
            _settings.ShowRunningAppsOnly = value;
        }
    }

    public bool BlurEffect
    {
        get => _blurEffect;
        set
        {
            if (_blurEffect == value)
                return;
            
            _blurEffect = value;
            OnPropertyChanged();
            _settings.BlurEffect = value;
        }
    }

    public bool EnableAnimations
    {
        get => _enableAnimations;
        set
        {
            if (_enableAnimations == value)
                return;
            
            _enableAnimations = value;
            OnPropertyChanged();
            _settings.EnableAnimations = value;
        }
    }

    public bool ShowWindowPreview
    {
        get => _showWindowPreview;
        set
        {
            if (_showWindowPreview == value)
                return;
            
            _showWindowPreview = value;
            OnPropertyChanged();
            _settings.ShowWindowPreview = value;
        }
    }

    public DockLauncherSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
        
        _settings = new DockSettings();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            await _settings.LoadAsync();
            
            IconSize = _settings.IconSize;
            DockHeight = _settings.DockHeight;
            Opacity = _settings.Opacity;
            PositionIndex = (int)_settings.Position;
            AutoHide = _settings.AutoHide;
            AutoHideDelay = _settings.AutoHideDelay;
            AutoAddTaskbarApps = _settings.AutoAddTaskbarApps;
            ShowRunningAppsOnly = _settings.ShowRunningAppsOnly;
            BlurEffect = _settings.BlurEffect;
            EnableAnimations = _settings.EnableAnimations;
            ShowWindowPreview = _settings.ShowWindowPreview;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.DockLauncher.Services.Settings;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Dock launcher settings page
/// </summary>
public partial class DockLauncherSettingsPage : INotifyPropertyChanged
{
    private readonly DockSettings _settings;
    private int _iconSize;
    private bool _autoHide;
    private int _autoHideDelay;

    public int IconSize
    {
        get => _iconSize;
        set
        {
            if (_iconSize == value)
                return;
            
            _iconSize = value;
            OnPropertyChanged();
            _settings.IconSize = value;
        }
    }

    public bool AutoHide
    {
        get => _autoHide;
        set
        {
            if (_autoHide == value)
                return;
            
            _autoHide = value;
            OnPropertyChanged();
            _settings.AutoHide = value;
        }
    }

    public int AutoHideDelay
    {
        get => _autoHideDelay;
        set
        {
            if (_autoHideDelay == value)
                return;
            
            _autoHideDelay = value;
            OnPropertyChanged();
            _settings.AutoHideDelay = value;
        }
    }

    public DockLauncherSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
        
        _settings = new DockSettings();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            await _settings.LoadAsync();
            
            IconSize = _settings.IconSize;
            AutoHide = _settings.AutoHide;
            AutoHideDelay = _settings.AutoHideDelay;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

