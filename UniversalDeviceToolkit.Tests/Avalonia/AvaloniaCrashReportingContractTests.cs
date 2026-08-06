using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaCrashReportingContractTests
{
    [Fact]
    public void AvaloniaHost_ShouldPersistAndRecoverCrashReportsLikeWpf()
    {
        var root = RepositoryPaths.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));
        var store = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Lib.Shared", "Diagnostics", "CrashReportStore.cs"));
        var window = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Windows", "AvaloniaCrashReportWindow.cs"));

        app.Should().Contain("AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException");
        app.Should().Contain("TaskScheduler.UnobservedTaskException += OnUnobservedTaskException");
        app.Should().Contain("Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException");
        app.Should().Contain("CrashReportStore.Save(exception, source)");
        app.Should().Contain("CrashReportStore.Save(args.Exception, \"TaskScheduler\")");
        app.Should().Contain("CheckPendingCrashReports");

        store.Should().Contain("crash_reports");
        store.Should().Contain("JsonSerializer.Serialize(report");
        store.Should().Contain("JsonSerializer.Deserialize<CrashReport>");
        window.Should().Contain("CrashReportStore.Load(reportPath)");
        window.Should().Contain("CrashReportStore.Delete(_reportPath)");
        window.Should().Contain("CrashReportNotification_OpenReport");
    }
}
