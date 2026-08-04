using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Shared.Settings;
using UniversalDeviceToolkit.Platform.Linux;
using UniversalDeviceToolkit.Platform.MacOS;
#if WINDOWS
using Autofac;
using WindowsDeviceAdapter = UniversalDeviceToolkit.Platform.Windows.WindowsDeviceAdapter;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.CLI;
#endif

namespace UniversalDeviceToolkit.Avalonia;

public partial class App : Application
{
    public ICommand ShowCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ExitCommand { get; }

    private TrayIcon? _trayIcon;

#if WINDOWS
    private ApplicationSettings? _applicationSettings;
#endif

    public static IPlatformServices PlatformServices { get; private set; } = new UnavailablePlatformServices();

    public App()
    {
        var culture = LocalizationRuntime.Initialize();
        AvaloniaLocalization.ApplyCulture(culture);
        LocalizationRuntime.CultureChanged += OnCultureChanged;
#if WINDOWS
        _applicationSettings = WindowsAvaloniaSettingsService.SharedApplicationSettings;
        InitializeWindowsServices(_applicationSettings);
#endif
        PlatformServices = CreatePlatformServices();
        ShowCommand = new RelayCommand(ShowMainWindow);
        SettingsCommand = new RelayCommand(OpenSettings);
        ExitCommand = new RelayCommand(ExitApplication);
        DataContext = this;
    }

    private static IPlatformServices CreatePlatformServices()
    {
#if WINDOWS
        return WindowsPlatformServices.Create();
#else
        if (OperatingSystem.IsLinux())
            return new DeviceAdapterPlatformServices(new LinuxDeviceAdapter());

        if (OperatingSystem.IsMacOS())
            return new DeviceAdapterPlatformServices(new MacOSDeviceAdapter());

        return new UnavailablePlatformServices();
#endif
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ApplyPersistedTheme();
#if WINDOWS
            _applicationSettings ??= WindowsAvaloniaSettingsService.SharedApplicationSettings;
#endif
            desktop.MainWindow = new MainWindow(PlatformServices);
            // Minimize to tray instead of closing
            desktop.MainWindow.Closing += OnMainWindowClosing;

            // Set up system tray icon programmatically
            SetupTrayIcon();
#if WINDOWS
            _ = StartWindowsHostServicesAsync();
#endif
        }
        base.OnFrameworkInitializationCompleted();
    }

#if WINDOWS
    private static void InitializeWindowsServices(ApplicationSettings settings)
    {
        try
        {
            if (IoCContainer.TryResolve<ApplicationSettings>() is not null)
                return;

            IoCContainer.Initialize(
                builder =>
                {
                    builder.RegisterInstance(settings).As<ApplicationSettings>().SingleInstance();
                    builder.RegisterType<AvaloniaMainThreadDispatcher>()
                        .As<IMainThreadDispatcher>()
                        .SingleInstance();
                    builder.RegisterType<IpcServer>()
                        .AsSelf()
                        .As<UniversalDeviceToolkit.Abstractions.Lifecycle.ICliHostLifecycle>()
                        .SingleInstance();
                },
                new UniversalDeviceToolkit.Lib.IoCModule(),
                new UniversalDeviceToolkit.Lib.Plugins.IoCModule(),
                new UniversalDeviceToolkit.Lib.Automation.IoCModule(),
                new UniversalDeviceToolkit.Lib.Macro.IoCModule(),
                new UniversalDeviceToolkit.WPF.IoCModule());
        }
        catch
        {
            // A host embedding Avalonia may have already initialized the shared container.
            // The feature bridge will fall back to adapter-only state when resolution fails.
        }
    }
#endif

    private void ApplyPersistedTheme()
    {
        var theme = new AvaloniaThemePreferences().Store.Theme;
        RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void SetupTrayIcon()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;

        var menu = new NativeMenu
        {
            Items =
            {
                new NativeMenuItem(AvaloniaLocalization.GetString("Nav_Show", "Show")) { Command = ShowCommand },
                new NativeMenuItem(AvaloniaLocalization.GetString("Nav_Settings", "Settings")) { Command = SettingsCommand },
                new NativeMenuItemSeparator(),
                new NativeMenuItem(AvaloniaLocalization.GetString("Nav_Exit", "Exit")) { Command = ExitCommand },
            }
        };

        _trayIcon = new TrayIcon
        {
            ToolTipText = AvaloniaLocalization.GetString("Window_Title", "Universal Device Toolkit"),
            Menu = menu,
        };

        // Try to load icon from Avalonia resource; falls back gracefully if unavailable
        try
        {
            var uri = new Uri("avares://UniversalDeviceToolkit.Avalonia/Assets/udt-icon.ico");
            _trayIcon.Icon = new WindowIcon(new global::Avalonia.Media.Imaging.Bitmap(AssetLoader.Open(uri)));
        }
        catch
        {
            // Icon resource not found; tray icon will display without a custom icon
        }
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        AvaloniaLocalization.ApplyCulture(e.Culture);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is MainWindow mainWindow)
        {
            mainWindow.RefreshForCulture();
            SetupTrayIcon();
        }
    }

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is Window window && !IsExiting)
        {
#if WINDOWS
            var settings = _applicationSettings?.Store;
            if (settings?.MinimizeOnClose == true)
            {
                e.Cancel = true;
                window.WindowState = WindowState.Minimized;
                return;
            }

            if (settings?.MinimizeToTray == true)
            {
                e.Cancel = true;
                window.Hide();
                return;
            }
#endif
        }
    }

    internal bool MinimizeToTrayEnabled
    {
        get
        {
#if WINDOWS
            return _applicationSettings?.Store.MinimizeToTray == true;
#else
            return false;
#endif
        }
    }

    private bool IsExiting { get; set; }

    private void ShowMainWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow == null) return;
            
            // Restore window state if minimized
            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }
            
            mainWindow.Show();
            mainWindow.Activate();
            
            // Force UI refresh after restore
            mainWindow.InvalidateVisual();
        }
    }

    private void OpenSettings()
    {
        ShowMainWindow();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is MainWindow mainWindow)
        {
            mainWindow.ShowSettingsPage();
        }
    }

    private void ExitApplication()
    {
        IsExiting = true;
#if WINDOWS
        try
        {
            IoCContainer.TryResolve<UniversalDeviceToolkit.Abstractions.Lifecycle.ICliHostLifecycle>()?
                .StopAsync()
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
            // Process teardown remains best effort; the named pipe is scoped to this process.
        }
#endif
        _trayIcon?.Dispose();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

#if WINDOWS
    private static async Task StartWindowsHostServicesAsync()
    {
        try
        {
            var lifecycle = IoCContainer.TryResolve<UniversalDeviceToolkit.Abstractions.Lifecycle.ICliHostLifecycle>();
            if (lifecycle is not null)
                await lifecycle.StartStopIfNeededAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Avalonia Windows host service startup failed.", ex);
        }
    }
#endif
}

/// <summary>
/// Simple ICommand implementation for tray menu commands.
/// </summary>
internal sealed class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
