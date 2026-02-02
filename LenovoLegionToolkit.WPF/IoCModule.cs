using Autofac;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.WPF.CLI;
using LenovoLegionToolkit.WPF.Settings;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.ViewModels;

using LenovoLegionToolkit.WPF.Windows;

namespace LenovoLegionToolkit.WPF;

public class IoCModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register<MainWindow>();
        builder.Register<MainThreadDispatcher>();

        builder.Register<SpectrumScreenCapture>();

        builder.Register<ThemeManager>().AutoActivate();
        builder.Register<NotificationsManager>().AutoActivate();

        builder.Register<DashboardSettings>();

        builder.Register<IpcServer>();

        builder.Register<WindowsOptimizationViewModel>();
    }
}
