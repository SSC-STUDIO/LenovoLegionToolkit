using Autofac;
using UniversalDeviceToolkit.Lib.Automation.Optimization;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Avalonia.Settings;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.ViewModels;

using UniversalDeviceToolkit.Avalonia.Windows;

namespace UniversalDeviceToolkit.Avalonia;

public class IoCModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register<MainWindow>();
        builder.Register<MainThreadDispatcher>();

        builder.Register<LanguagePackManager>();
        builder.Register<LanguagePackInstallCoordinator>().SingleInstance();
        builder.Register<PluginInstallCoordinator>().SingleInstance();

        builder.Register<ThemeManager>().AutoActivate();
        builder.Register<NotificationsManager>().AutoActivate();

        builder.Register<DashboardSettings>().SingleInstance();
        builder.Register<HardwareSensorSettings>()
            .AsSelf()
            .As<UniversalDeviceToolkit.Lib.Settings.HardwareSensorSettings>()
            .SingleInstance();

        builder.RegisterModule(new WindowsOptimizationElevationIoCModule());

        builder.Register<UniversalDeviceToolkit.ViewModels.KeyboardBacklightViewModel>();
        builder.Register<WindowsOptimizationViewModel>();
    }
}
