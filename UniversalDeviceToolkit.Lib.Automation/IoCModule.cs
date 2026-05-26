using Autofac;
using UniversalDeviceToolkit.Lib.Automation.Utils;
using LenovoLegionToolkit.Lib.Extensions;

namespace UniversalDeviceToolkit.Lib.Automation;

public class IoCModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register<AutomationSettings>();
        builder.Register<AutomationProcessor>();
    }
}
