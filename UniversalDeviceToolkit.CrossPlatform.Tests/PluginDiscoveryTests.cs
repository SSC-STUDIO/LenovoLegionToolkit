using FluentAssertions;
using System.Text.RegularExpressions;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class PluginDiscoveryTests
{
    [Fact]
    public void Read_ShouldInspectManifestWithoutLoadingAssemblies()
    {
        var root = FullPath("/plugins");
        var fileSystem = new FakeFileSystem(new Dictionary<string, string>
        {
            [FullPath("/plugins/cross/plugin.manifest.json")] = """
                {
                  "id": "cross",
                  "name": "Cross Platform Plugin",
                  "version": "2.1.0",
                  "targetPlatforms": [ "linux", "macOS" ],
                  "contributes": {
                    "runtime": { "class": "Cross.Plugin.Runtime" },
                    "optimizationActions": [
                      { "id": "cross.clean" },
                      { "id": "cross.profile" }
                    ]
                  }
                }
                """,
            [FullPath("/plugins/cross/Cross.Plugin.dll")] = "not loaded",
            [FullPath("/plugins/windows/Plugin.json")] = """
                {
                  "id": "windows-only",
                  "name": "Windows Only",
                  "version": "1.0.0"
                }
                """
        });

        var report = new PluginDiscoveryReader(fileSystem, root).Read();

        report.Source.Should().Be("cross-platform-plugin-manifest");
        report.SearchRoots.Should().ContainSingle().Which.Should().Be(root);
        report.Plugins.Should().HaveCount(2);
        report.Plugins.Should().ContainEquivalentOf(new PluginDescriptor(
            "cross",
            "Cross Platform Plugin",
            "2.1.0",
            FullPath("/plugins/cross/plugin.manifest.json"),
            true,
            true,
            2,
            ["linux", "macos"],
            "targets linux, macos; declares runtime contribution; declares 2 optimization actions"));
        report.Plugins.Should().Contain(plugin =>
            plugin.Id == "windows-only" &&
            !plugin.IsCrossPlatformCandidate &&
            plugin.Reason.Contains("no non-Windows", StringComparison.OrdinalIgnoreCase));
        report.Notes.Should().ContainSingle(note => note.Contains("not loaded", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Read_WhenManifestHasOptimizationActions_ShouldTreatAsCandidate()
    {
        var root = FullPath("/plugins");
        var fileSystem = new FakeFileSystem(new Dictionary<string, string>
        {
            [FullPath("/plugins/optimizer/plugin.json")] = """
                {
                  "id": "optimizer",
                  "name": "Optimizer",
                  "contributes": {
                    "optimizationActions": [
                      { "id": "optimizer.safe" }
                    ]
                  }
                }
                """
        });

        var plugin = new PluginDiscoveryReader(fileSystem, root).Read().Plugins.Single();

        plugin.IsCrossPlatformCandidate.Should().BeTrue();
        plugin.OptimizationActionCount.Should().Be(1);
        plugin.Reason.Should().Contain("optimization actions");
    }

    [Fact]
    public void Read_WhenManifestIsInvalid_ShouldReportPluginAsSkipped()
    {
        var root = FullPath("/plugins");
        var fileSystem = new FakeFileSystem(new Dictionary<string, string>
        {
            [FullPath("/plugins/broken/plugin.manifest.json")] = "{ invalid json"
        });

        var plugin = new PluginDiscoveryReader(fileSystem, root).Read().Plugins.Single();

        plugin.Id.Should().Be("broken");
        plugin.IsCrossPlatformCandidate.Should().BeFalse();
        plugin.Reason.Should().Contain("could not be parsed");
    }

    [Fact]
    public void Read_WhenNoManifestsExist_ShouldReturnNote()
    {
        var report = new PluginDiscoveryReader(new FakeFileSystem(new Dictionary<string, string>()), FullPath("/plugins")).Read();

        report.Plugins.Should().BeEmpty();
        report.Notes.Should().ContainSingle(note => note.Contains("No plugin manifests", StringComparison.OrdinalIgnoreCase));
    }

    private static string FullPath(string path) =>
        Path.GetFullPath(path.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar));

    private sealed class FakeFileSystem(IReadOnlyDictionary<string, string> files) : IFileSystem
    {
        public string ReadAllText(string path) => files.TryGetValue(path, out var value) ? value : string.Empty;

        public IEnumerable<string> EnumerateDirectories(string path) =>
            files.Keys
                .Where(file => file.StartsWith(path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Select(file =>
                {
                    var relativePath = file[(path.TrimEnd(Path.DirectorySeparatorChar).Length + 1)..];
                    var separator = relativePath.IndexOf(Path.DirectorySeparatorChar);
                    return separator < 0 ? string.Empty : $"{path.TrimEnd(Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}{relativePath[..separator]}";
                })
                .Where(directory => !string.IsNullOrWhiteSpace(directory))
                .Distinct(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            var regex = new Regex(
                "^" + Regex.Escape(searchPattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
                RegexOptions.IgnoreCase);

            return files.Keys
                .Where(file => file.StartsWith(path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Where(file => !file[(path.TrimEnd(Path.DirectorySeparatorChar).Length + 1)..].Contains(Path.DirectorySeparatorChar))
                .Where(file => regex.IsMatch(Path.GetFileName(file)));
        }

        public bool DirectoryExists(string path) =>
            files.Keys.Any(file => file.StartsWith(path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }
}
