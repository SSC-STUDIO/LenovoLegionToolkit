using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class CrashReportStartupGuardTests
{
    [Fact]
    public void Startup_ShouldShowPendingCrashReportsAfterMainWindow()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Startup", "StartupOrchestrator.cs");
        var methodStart = source.IndexOf("private Task ShowMainWindowAsync()", System.StringComparison.Ordinal);
        var methodEnd = source.IndexOf("private Task InitializeOsdAsync()", System.StringComparison.Ordinal);
        methodStart.Should().BeGreaterThanOrEqualTo(0);
        methodEnd.Should().BeGreaterThan(methodStart);

        var method = source[methodStart..methodEnd];
        method.Should().Contain("Show();");
        method.Should().Contain("Application.Current.Dispatcher.BeginInvoke(App.CheckPendingCrashReports,");
        method.IndexOf("Show();", System.StringComparison.Ordinal)
            .Should()
            .BeLessThan(method.IndexOf("Application.Current.Dispatcher.BeginInvoke(App.CheckPendingCrashReports,", System.StringComparison.Ordinal));
    }

    [Fact]
    public void PendingCrashReportNotification_ShouldNotBlockOrStayTopmost()
    {
        var appSource = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "App.xaml.cs");
        var checkMethodStart = appSource.IndexOf("static void CheckPendingCrashReports()", System.StringComparison.Ordinal);
        var nextMethodStart = appSource.IndexOf("void StartBackgroundInitialization()", System.StringComparison.Ordinal);
        checkMethodStart.Should().BeGreaterThanOrEqualTo(0);
        nextMethodStart.Should().BeGreaterThan(checkMethodStart);

        var checkMethod = appSource[checkMethodStart..nextMethodStart];
        checkMethod.Should().Contain("notificationWindow.Show();");
        checkMethod.Should().NotContain("ShowDialog()");

        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Utils", "CrashReportNotificationWindow.xaml");
        xaml.Should().NotContain("Topmost=\"True\"");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var expectedRelativePath = Path.Combine(pathParts);
        foreach (var candidateRoot in GetRepositoryRootCandidates())
        {
            var path = Path.Combine(candidateRoot, expectedRelativePath);
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new DirectoryNotFoundException($"Could not locate repository file '{expectedRelativePath}'.");
    }

    private static IEnumerable<string> GetRepositoryRootCandidates()
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
        };

        foreach (var root in roots.Where(static root => !string.IsNullOrWhiteSpace(root)))
        {
            var directory = new DirectoryInfo(root!);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                    yield return directory.FullName;

                directory = directory.Parent;
            }
        }
    }
}
