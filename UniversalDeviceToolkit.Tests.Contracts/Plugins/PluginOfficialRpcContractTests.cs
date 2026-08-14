using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Guard)]
public sealed class PluginOfficialRpcContractTests
{
    [Fact]
    public void PluginOfficialHandlers_ShouldRegisterExpectedMethods()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Host", "Rpc", "Handlers", "PluginOfficialHandlers.cs");

        foreach (var method in Methods("customMouse"))
            source.Should().Contain($"RegisterHandler(\"{method}\"");

        foreach (var method in Methods("shell"))
            source.Should().Contain($"RegisterHandler(\"{method}\"");

        foreach (var method in Methods("vive"))
            source.Should().Contain($"RegisterHandler(\"{method}\"");

        foreach (var name in Events())
            source.Should().Contain(name);
    }

    [Fact]
    public void PluginHandlers_ListProjection_ShouldIncludeDirectoryAndWebPage()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Host", "Rpc", "Handlers", "PluginHandlers.cs");

        source.Should().Contain("directory = ResolvePluginDirectory(metadata)");
        source.Should().Contain("webPage = webPage is { Entry.Length: > 0 }");
        source.Should().Contain("private static object ProjectInstalledOnlyView");
    }

    [Fact]
    public void PluginMutationHandlers_ShouldExposeStructuredDegradedOutcomes()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Host", "Rpc", "Handlers", "PluginHandlers.cs");

        source.Should().Contain("DownloadAndInstallPluginWithOutcomeAsync");
        source.Should().Contain("ScanAndLoadPluginsWithOutcomeAsync");
        source.Should().Contain("degraded = outcome.Degraded");
        source.Should().Contain("unloadPending = outcome.UnloadPending");
        source.Should().Contain("recoveryId = outcome.RecoveryId");
        source.Should().Contain("failures = outcome.Failures");
        source.Should().Contain("ExtractAndInstallPluginWithOutcomeAsync");
        source.Should().NotContain("ex.Message.Contains");
    }

    [Theory]
    [InlineData("CustomMouse", "custom-mouse", "plugin.customMouse.")]
    [InlineData("ShellIntegration", "shell-integration", "plugin.shell.")]
    [InlineData("ViveTool", "vive-tool", "plugin.vive.")]
    public void OfficialPlugin_ShouldShipWebPageAndInvokeRegisteredMethods(
        string folder,
        string pluginId,
        string methodPrefix)
    {
        var root = RepositoryPaths.Combine("Plugins", "Official", folder);
        var manifestPath = Path.Combine(root, "plugin.manifest.json");
        var webPath = Path.Combine(root, "web", "index.html");
        var cssPath = Path.Combine(root, "web", "plugin-ui.css");

        File.Exists(manifestPath).Should().BeTrue();
        File.Exists(webPath).Should().BeTrue();
        File.Exists(cssPath).Should().BeTrue();

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var contributes = doc.RootElement.GetProperty("contributes");
        contributes.GetProperty("webPage").GetProperty("entry").GetString().Should().Be("web/index.html");
        contributes.GetProperty("settingsPage").ValueKind.Should().Be(JsonValueKind.Null);

        var html = File.ReadAllText(webPath);
        html.Should().Contain("pluginHost");
        html.Should().Contain(methodPrefix);

        var registered = new HashSet<string>(AllMethods(), StringComparer.Ordinal);
        var events = new HashSet<string>(Events(), StringComparer.Ordinal);

        foreach (Match match in Regex.Matches(
            html,
            $@"['""]({Regex.Escape(methodPrefix)}[A-Za-z0-9]+)['""]"))
        {
            var name = match.Groups[1].Value;
            if (events.Contains(name))
                continue;

            registered.Should().Contain(name);
        }

        var requiredFiles = doc.RootElement.GetProperty("package").GetProperty("requiredFiles");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in requiredFiles.EnumerateArray())
        {
            var name = item.GetString();
            if (!string.IsNullOrWhiteSpace(name))
                names.Add(name);
        }

        names.Should().Contain("web/index.html");
        names.Should().Contain("web/plugin-ui.css");
        doc.RootElement.GetProperty("id").GetString().Should().Be(pluginId);
    }

    [Fact]
    public void ShellManifest_ShouldNotDeclareNativeSettingsControl()
    {
        var manifest = RepositoryPaths.ReadFile(
            "Plugins", "Official", "ShellIntegration", "plugin.manifest.json");

        manifest.Should().NotContain("ShellIntegrationSettingsControl");
    }

    [Fact]
    public void ElectronContractTest_ShouldReadSharedRpcList()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Electron", "tests", "pluginOfficialContract.test.mjs");

        source.Should().Contain("Plugins/Official/plugin-rpc-contract.json");
    }

    private static string[] Methods(string group) => ReadStringArray("methods", group);

    private static string[] AllMethods()
    {
        using var doc = OpenContract();
        return doc.RootElement.GetProperty("methods").EnumerateObject()
            .SelectMany(property => property.Value.EnumerateArray())
            .Select(item => item.GetString() ?? string.Empty)
            .Where(name => name.Length > 0)
            .ToArray();
    }

    private static string[] Events() => ReadStringArray("events");

    private static string[] ReadStringArray(params string[] path)
    {
        using var doc = OpenContract();
        var current = doc.RootElement;
        foreach (var part in path)
            current = current.GetProperty(part);

        return current.EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(name => name.Length > 0)
            .ToArray();
    }

    private static JsonDocument OpenContract() =>
        JsonDocument.Parse(RepositoryPaths.ReadFile("Plugins", "Official", "plugin-rpc-contract.json"));
}
