using Autofac;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Host;

/// <summary>
/// Host-specific Autofac registrations: bridge infrastructure that the shared
/// Lib modules do not know about.
/// </summary>
public class BridgeModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<HeadlessMainThreadDispatcher>()
            .As<IMainThreadDispatcher>()
            .SingleInstance();
    }
}
