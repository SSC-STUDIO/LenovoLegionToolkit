using Autofac;
using UniversalDeviceToolkit.Lib.Automation.CLI;
using UniversalDeviceToolkit.Lib.Automation.Utils;
using UniversalDeviceToolkit.Lib.Extensions;

namespace UniversalDeviceToolkit.Lib.Automation;

public class IoCModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register<AutomationSettings>();
        builder.Register<AutomationProcessor>();
        builder.Register<IpcServer>()
            .AsSelf()
            .As<UniversalDeviceToolkit.Abstractions.Lifecycle.ICliHostLifecycle>()
            .SingleInstance();
    }
}
