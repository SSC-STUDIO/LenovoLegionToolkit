using PluginTooling.Core;
using Xunit;

namespace PluginTooling.Tests;

public class PluginVersionSynchronizerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _pluginDirectory;

    public PluginVersionSynchronizerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "udt-version-sync-tests", Guid.NewGuid().ToString("N"));
        _pluginDirectory = Path.Combine(_tempRoot, "Plugins", "Sample");
        Directory.CreateDirectory(_pluginDirectory);
        File.WriteAllText(Path.Combine(_tempRoot, "UniversalDeviceToolkit-Plugins.sln"), "Microsoft Visual Studio Solution File, Format Version 12.00\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("1.0.16", VersionBumpPart.Patch, "1.0.17")]
    [InlineData("1.2.2", VersionBumpPart.Minor, "1.3.0")]
    [InlineData("1.2.2", VersionBumpPart.Patch, "1.2.3")]
    [InlineData("2.4.9", VersionBumpPart.Major, "3.0.0")]
    public void BumpSemVer_UsesExpectedPart(string current, VersionBumpPart part, string expected)
    {
        Assert.Equal(expected, PluginVersionSynchronizer.BumpSemVer(current, part));
    }

    [Fact]
    public void Sync_WritesCsprojAndPluginAttribute_FromManifest()
    {
        WritePluginFixture(manifestVersion: "2.0.0", csprojVersion: "1.0.0", attributeVersion: "1.0.0");

        var repository = new PluginRepository();
        var context = repository.Load(_tempRoot).Plugins["sample-plugin"];
        var synchronizer = new PluginVersionSynchronizer();

        var report = synchronizer.Sync(context, writeChanges: true);

        Assert.True(report.Changed);
        Assert.Equal("2.0.0", report.ProjectVersion);
        Assert.Equal("2.0.0", report.PluginAttributeVersion);

        var pluginJson = File.ReadAllText(Path.Combine(_pluginDirectory, "plugin.json"));
        Assert.Contains("\"Version\": \"2.0.0\"", pluginJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sync_CheckOnly_ReportsDriftWithoutWriting()
    {
        WritePluginFixture(manifestVersion: "2.0.0", csprojVersion: "1.0.0", attributeVersion: "1.0.0");

        var repository = new PluginRepository();
        var context = repository.Load(_tempRoot).Plugins["sample-plugin"];
        var synchronizer = new PluginVersionSynchronizer();

        var report = synchronizer.Sync(context, writeChanges: false);

        Assert.False(report.Changed);
        Assert.False(report.IsAligned);
        Assert.Contains(report.DriftMessages, message => message.Contains("csproj Version", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("1.0.0", report.ProjectVersion);
    }

    private void WritePluginFixture(string manifestVersion, string csprojVersion, string attributeVersion)
    {
        var manifest = $$"""
        {
          "schemaVersion": 1,
          "id": "sample-plugin",
          "name": "Sample Plugin",
          "version": "{{manifestVersion}}",
          "minHostVersion": "4.2.1",
          "author": "SSC-STUDIO",
          "isSystemPlugin": false,
          "repository": "https://example.com/repo",
          "issues": "https://example.com/issues",
          "lifecycle": "Active",
          "contributes": {
            "featurePage": null,
            "settingsPage": {
              "class": "UniversalDeviceToolkit.Plugins.Sample.SampleSettingsPage",
              "title": "Sample"
            },
            "runtime": null,
            "optimizationActions": []
          },
          "package": {
            "assetName": "sample-plugin-v{{manifestVersion}}.zip",
            "requiredFiles": [
              "UniversalDeviceToolkit.Plugins.Sample.dll",
              "UniversalDeviceToolkit.Plugins.SDK.dll",
              "plugin.json",
              "plugin.manifest.json"
            ]
          },
          "store": {
            "description": "Sample plugin",
            "icon": "PuzzlePiece24",
            "iconBackground": "#2563EB",
            "tags": [ "sample" ],
            "dependencies": [],
            "supportedLanguages": [ "en" ],
            "repositoryUrl": "https://example.com/repo"
          }
        }
        """;

        File.WriteAllText(Path.Combine(_pluginDirectory, "plugin.manifest.json"), manifest);
        File.WriteAllText(Path.Combine(_pluginDirectory, "plugin.json"), $$"""
        {
          "Id": "sample-plugin",
          "Name": "Sample Plugin",
          "Version": "{{csprojVersion}}",
          "MinLltVersion": "4.2.1",
          "Author": "SSC-STUDIO",
          "IsSystemPlugin": false
        }
        """);

        File.WriteAllText(Path.Combine(_pluginDirectory, "UniversalDeviceToolkit.Plugins.Sample.csproj"), $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <Version>{{csprojVersion}}</Version>
            <FileVersion>{{csprojVersion}}</FileVersion>
            <AssemblyVersion>{{csprojVersion}}</AssemblyVersion>
            <AssemblyName>UniversalDeviceToolkit.Plugins.Sample</AssemblyName>
          </PropertyGroup>
        </Project>
        """);

        File.WriteAllText(Path.Combine(_pluginDirectory, "SamplePlugin.cs"), $$"""
        using UniversalDeviceToolkit.Plugins.SDK;

        namespace UniversalDeviceToolkit.Plugins.Sample;

        [Plugin(
            id: "sample-plugin",
            name: "Sample Plugin",
            version: "{{attributeVersion}}",
            description: "Sample",
            author: "SSC-STUDIO",
            MinimumHostVersion = "4.2.1",
            Icon = "PuzzlePiece24"
        )]
        public class SamplePlugin : PluginBase
        {
            public override string Id => "sample-plugin";
        }
        """);
    }
}