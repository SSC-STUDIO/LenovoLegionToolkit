using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public sealed class ManagementEventWatcherExtensionsSourceTests
{
    [Fact]
    public void StartWithTimeout_ShouldBePresent()
    {
        var source = ReadSourceFile();
        source.Should().Contain("public static void StartWithTimeout(");
    }

    [Fact]
    public void StartAsyncWithTimeout_ShouldBePresent()
    {
        var source = ReadSourceFile();
        source.Should().Contain("public static async Task StartAsyncWithTimeout(");
    }

    [Fact]
    public void StartWithTimeout_ShouldNotUseGetAwaiterGetResult()
    {
        var source = ReadSourceFile();
        source.Should().NotContain("GetAwaiter().GetResult()");
    }

    private static string ReadSourceFile()
    {
        var relativePath = Path.Combine("UniversalDeviceToolkit.Lib", "Extensions", "ManagementEventWatcherExtensions.cs");
        foreach (var candidateRoot in GetRepositoryRootCandidates())
        {
            var path = Path.Combine(candidateRoot, relativePath);
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new FileNotFoundException($"Could not locate source file '{relativePath}'.");
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
