using Autofac;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Utils;
using System.Threading;

namespace LenovoLegionToolkit.Tests;

public class TestIoCModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<TestMainThreadDispatcher>().As<IMainThreadDispatcher>().SingleInstance();
        
        // Register a mock implementation of ISpectrumScreenCapture for testing
        builder.RegisterType<TestSpectrumScreenCapture>().As<SpectrumKeyboardBacklightController.ISpectrumScreenCapture>().SingleInstance();
    }
}

internal class TestSpectrumScreenCapture : SpectrumKeyboardBacklightController.ISpectrumScreenCapture
{
    public void CaptureScreen(ref Lib.RGBColor[,] buffer, int width, int height, CancellationToken token)
    {
        // Mock implementation - do nothing for testing
    }
}
