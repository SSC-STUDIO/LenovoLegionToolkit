using System.IO.Compression;
using PluginTooling.Core;
using Xunit;

namespace PluginTooling.Tests;

public sealed class PluginPackagerTests : IDisposable
{
    private readonly string _root;
    private readonly string _pluginDirectory;
    private readonly string _outputDirectory;

    public PluginPackagerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "udt-packager-tests", Guid.NewGuid().ToString("N"));
        _pluginDirectory = Path.Combine(_root, "Official", "Sample");
        _outputDirectory = Path.Combine(_root, ".build", "plugins", "UniversalDeviceToolkit.Plugins.Sample");

        Directory.CreateDirectory(_pluginDirectory);
        Directory.CreateDirectory(_outputDirectory);
        File.WriteAllText(
            Path.Combine(_root, "UniversalDeviceToolkit.Plugins.sln"),
            "Microsoft Visual Studio Solution File, Format Version 12.00\n");

        File.WriteAllText(Path.Combine(_pluginDirectory, "plugin.manifest.json"),
            """
            {
              "schemaVersion": 1,
              "id": "sample-plugin",
              "name": "Sample Plugin",
              "version": "1.0.0",
              "minHostVersion": "5.0.0",
              "author": "Test",
              "isSystemPlugin": false,
              "repository": "https://example.com/sample-plugin",
              "issues": "https://example.com/sample-plugin/issues",
              "package": {
                "assetName": "sample-plugin-v1.0.0.zip",
                "requiredFiles": [
                  "UniversalDeviceToolkit.Plugins.Sample.dll",
                  "plugin.json",
                  "plugin.manifest.json"
                ]
              },
              "store": {
                "description": "Sample plugin",
                "icon": "PuzzlePiece24",
                "iconBackground": "#FFFFFF",
                "tags": ["test"],
                "dependencies": [],
                "supportedLanguages": ["en"]
              }
            }
            """);
        File.WriteAllText(Path.Combine(_pluginDirectory, "plugin.json"),
            """
            {
              "Id": "sample-plugin",
              "Name": "Sample Plugin",
              "Version": "1.0.0",
              "MinLltVersion": "5.0.0",
              "Author": "Test",
              "IsSystemPlugin": false
            }
            """);

        File.WriteAllText(Path.Combine(_outputDirectory, "UniversalDeviceToolkit.Plugins.Sample.dll"), "plugin");
        File.Copy(
            Path.Combine(_pluginDirectory, "plugin.json"),
            Path.Combine(_outputDirectory, "plugin.json"));
        File.Copy(
            Path.Combine(_pluginDirectory, "plugin.manifest.json"),
            Path.Combine(_outputDirectory, "plugin.manifest.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PackAsync_ShouldExcludeHostRuntimeFilesFromZip()
    {
        File.WriteAllText(Path.Combine(_outputDirectory, "UniversalDeviceToolkit.Lib.dll"), "host");
        File.WriteAllText(Path.Combine(_outputDirectory, "UniversalDeviceToolkit.Lib.pdb"), "host symbols");
        File.WriteAllText(Path.Combine(_outputDirectory, "Universal Device Toolkit.dll"), "host wpf");

        var packageDirectory = Path.Combine(_root, "release-assets");
        var result = await new PluginPackager().PackAsync(new PackRequest
        {
            RepositoryRoot = _root,
            PluginId = "sample-plugin",
            OutputDirectory = packageDirectory,
        });

        using var archive = ZipFile.OpenRead(result.ZipPath);
        var entryNames = archive.Entries.Select(entry => entry.FullName).ToArray();

        Assert.Contains("UniversalDeviceToolkit.Plugins.Sample.dll", entryNames);
        Assert.Contains("plugin.json", entryNames);
        Assert.Contains("plugin.manifest.json", entryNames);
        Assert.DoesNotContain("UniversalDeviceToolkit.Lib.dll", entryNames);
        Assert.DoesNotContain("UniversalDeviceToolkit.Lib.pdb", entryNames);
        Assert.DoesNotContain("Universal Device Toolkit.dll", entryNames);
    }
}
