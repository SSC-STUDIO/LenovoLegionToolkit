using Autofac;
using LenovoLegionToolkit.Lib.Extensions;
using UniversalDeviceToolkit.WPF.CLI;
using UniversalDeviceToolkit.WPF.Settings;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.ViewModels;

using UniversalDeviceToolkit.WPF.Windows;

namespace UniversalDeviceToolkit.WPF;

public class IoCModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register<MainWindow>();
        builder.Register<MainThreadDispatcher>();

        builder.Register<SpectrumScreenCapture>();
        builder.Register<LanguagePackManager>();
        builder.Register<LanguagePackInstallCoordinator>().SingleInstance();
        builder.Register<PluginInstallCoordinator>().SingleInstance();

        builder.Register<ThemeManager>().AutoActivate();
        builder.Register<NotificationsManager>().AutoActivate();

        builder.Register<DashboardSettings>().SingleInstance();
        builder.Register<HardwareSensorSettings>();

        builder.Register<IpcServer>().SingleInstance();

        builder.Register<WindowsOptimizationViewModel>();
    }
}
