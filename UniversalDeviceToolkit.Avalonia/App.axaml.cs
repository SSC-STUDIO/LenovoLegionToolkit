using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Platform.Linux;
using UniversalDeviceToolkit.Platform.MacOS;
#if WINDOWS
using WindowsDeviceAdapter = UniversalDeviceToolkit.Platform.Windows.WindowsDeviceAdapter;
#endif

namespace UniversalDeviceToolkit.Avalonia;

public partial class App : Application
{
    public ICommand ShowCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ExitCommand { get; }

    private TrayIcon? _trayIcon;

    public static IPlatformServices PlatformServices { get; private set; } = new SamplePlatformServices();

    public App()
    {
        var culture = LocalizationRuntime.Initialize();
        AvaloniaLocalization.ApplyCulture(culture);
        LocalizationRuntime.CultureChanged += OnCultureChanged;
        PlatformServices = CreatePlatformServices();
        ShowCommand = new RelayCommand(ShowMainWindow);
        SettingsCommand = new RelayCommand(OpenSettings);
        ExitCommand = new RelayCommand(ExitApplication);
        DataContext = this;
    }

    private static IPlatformServices CreatePlatformServices()
    {
#if WINDOWS
        return new DeviceAdapterPlatformServices(new WindowsDeviceAdapter());
#else
        if (OperatingSystem.IsLinux())
            return new PlatformCapabilityServices(new LinuxPlatformServices());

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
            desktop.MainWindow = new MainWindow(PlatformServices);
            // Minimize to tray instead of closing
            desktop.MainWindow.Closing += OnMainWindowClosing;

            // Set up system tray icon programmatically
            SetupTrayIcon();
        }
        base.OnFrameworkInitializationCompleted();
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
            e.Cancel = true;
            window.Hide();
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
        _trayIcon?.Dispose();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
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
