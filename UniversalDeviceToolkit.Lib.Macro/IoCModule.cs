using Autofac;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Macro.Utils;

namespace UniversalDeviceToolkit.Lib.Macro;

public class IoCModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register<MacroSettings>();
        builder.Register<MacroController>();
    }
}
