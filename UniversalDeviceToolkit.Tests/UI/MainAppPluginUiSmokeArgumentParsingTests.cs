using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.UI;

[Trait("Category", TestCategories.Smoke)]
public class MainAppPluginUiSmokeArgumentParsingTests
{
    [Fact]
    public void ResolveRepositoryRoot_WithNamedArguments_ReturnsExplicitRepoRoot()
    {
        var repoRoot = RepositoryPaths.FindRoot();
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveRepositoryRoot = programType.GetMethod("ResolveRepositoryRoot", BindingFlags.NonPublic | BindingFlags.Static);

        resolveRepositoryRoot.Should().NotBeNull();

        var result = (string?)resolveRepositoryRoot!.Invoke(
            null,
            new object[]
            {
                new[]
                {
                    "--repo-root",
                    repoRoot,
                    "--plugin",
                    "shell-integration",
                    "--culture",
                    "zh-Hans"
                }
            });

        result.Should().Be(repoRoot);
    }

    [Fact]
    public void ResolveRepositoryRoot_WithPositionalArgument_ReturnsExplicitRepoRoot()
    {
        var repoRoot = RepositoryPaths.FindRoot();
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveRepositoryRoot = programType.GetMethod("ResolveRepositoryRoot", BindingFlags.NonPublic | BindingFlags.Static);

        resolveRepositoryRoot.Should().NotBeNull();

        var result = (string?)resolveRepositoryRoot!.Invoke(
            null,
            new object[] { new[] { repoRoot } });

        result.Should().Be(repoRoot);
    }

    [Fact]
    public void ResolveScenario_WithShellLocal_ReturnsShellLocalScenario()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveScenario = programType.GetMethod("ResolveScenario", BindingFlags.NonPublic | BindingFlags.Static);

        resolveScenario.Should().NotBeNull();

        var result = resolveScenario!.Invoke(null, new object[] { new[] { "--scenario", "shell-local" } });

        result.Should().NotBeNull();
        result!.ToString().Should().Be("ShellLocal");
    }

    [Fact]
    public void ResolveScenario_WithComboLocal_ReturnsComboLocalScenario()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveScenario = programType.GetMethod("ResolveScenario", BindingFlags.NonPublic | BindingFlags.Static);

        resolveScenario.Should().NotBeNull();

        var result = resolveScenario!.Invoke(null, new object[] { new[] { "--scenario", "combo-local" } });

        result.Should().NotBeNull();
        result!.ToString().Should().Be("ComboLocal");
    }

    [Fact]
    public void ResolveScenario_WithNoArgument_ReturnsNoneScenario()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveScenario = programType.GetMethod("ResolveScenario", BindingFlags.NonPublic | BindingFlags.Static);

        resolveScenario.Should().NotBeNull();

        var result = resolveScenario!.Invoke(null, new object[] { Array.Empty<string>() });

        result.Should().NotBeNull();
        result!.ToString().Should().Be("None");
    }

    [Fact]
    public void ResolveTheme_WithDark_ReturnsDarkTheme()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveTheme = programType.GetMethod("ResolveTheme", BindingFlags.NonPublic | BindingFlags.Static);

        resolveTheme.Should().NotBeNull();

        var result = resolveTheme!.Invoke(null, new object[] { new[] { "--theme", "dark" } });

        result.Should().NotBeNull();
        result!.ToString().Should().Be("Dark");
    }

    [Fact]
    public void ResolveTheme_WithLight_ReturnsLightTheme()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveTheme = programType.GetMethod("ResolveTheme", BindingFlags.NonPublic | BindingFlags.Static);

        resolveTheme.Should().NotBeNull();

        var result = resolveTheme!.Invoke(null, new object[] { new[] { "--theme", "light" } });

        result.Should().NotBeNull();
        result!.ToString().Should().Be("Light");
    }

    [Fact]
    public void ResolveTheme_WithNoArgument_ReturnsSystemTheme()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveTheme = programType.GetMethod("ResolveTheme", BindingFlags.NonPublic | BindingFlags.Static);

        resolveTheme.Should().NotBeNull();

        var result = resolveTheme!.Invoke(null, new object[] { Array.Empty<string>() });

        result.Should().NotBeNull();
        result!.ToString().Should().Be("System");
    }

    [Fact]
    public void ResolveScreenshotMode_WithAlways_ReturnsAlwaysMode()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveScreenshotMode = programType.GetMethod("ResolveScreenshotMode", BindingFlags.NonPublic | BindingFlags.Static);

        resolveScreenshotMode.Should().NotBeNull();

        var result = resolveScreenshotMode!.Invoke(null, new object[] { new[] { "--screenshots", "always" } });

        result.Should().NotBeNull();
        result!.ToString().Should().Be("Always");
    }

    [Fact]
    public void ResolveScreenshotMode_WithOff_ReturnsOffMode()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveScreenshotMode = programType.GetMethod("ResolveScreenshotMode", BindingFlags.NonPublic | BindingFlags.Static);

        resolveScreenshotMode.Should().NotBeNull();

        var result = resolveScreenshotMode!.Invoke(null, new object[] { new[] { "--screenshots", "off" } });

        result.Should().NotBeNull();
        result!.ToString().Should().Be("Off");
    }

    [Fact]
    public void ResolveScreenshotMode_WithNoArgument_ReturnsFailuresMode()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolveScreenshotMode = programType.GetMethod("ResolveScreenshotMode", BindingFlags.NonPublic | BindingFlags.Static);

        resolveScreenshotMode.Should().NotBeNull();

        var result = resolveScreenshotMode!.Invoke(null, new object[] { Array.Empty<string>() });

        result.Should().NotBeNull();
        result!.ToString().Should().Be("Failures");
    }

    [Fact]
    public void ResolvePreferredPlugins_WithPluginArgument_ReturnsSpecifiedPlugins()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var resolvePreferredPlugins = programType.GetMethod("ResolvePreferredPlugins", BindingFlags.NonPublic | BindingFlags.Static);

        resolvePreferredPlugins.Should().NotBeNull();

        var scenarioPresetType = programType.Assembly.GetType("MainAppPluginUi.Smoke.Program+ScenarioPreset");
        var result = resolvePreferredPlugins!.Invoke(null, new object?[] { new[] { "--plugin", "custom-mouse,vive-tool" }, null });

        result.Should().NotBeNull();
        var resultArray = (System.Collections.IList)result!;
        resultArray.Count.Should().Be(2);
    }

    [Fact]
    public void ParseBooleanLikeValue_WithTrueValues_ReturnsTrue()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var parseBooleanLikeValue = programType.GetMethod("ParseBooleanLikeValue", BindingFlags.NonPublic | BindingFlags.Static);

        parseBooleanLikeValue.Should().NotBeNull();

        var trueValues = new[] { "true", "True", "TRUE", "1", "yes", "on" };
        foreach (var value in trueValues)
        {
            var result = parseBooleanLikeValue!.Invoke(null, new object[] { value, "test" });
            result.Should().Be(true);
        }
    }

    [Fact]
    public void ParseBooleanLikeValue_WithFalseValues_ReturnsFalse()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var parseBooleanLikeValue = programType.GetMethod("ParseBooleanLikeValue", BindingFlags.NonPublic | BindingFlags.Static);

        parseBooleanLikeValue.Should().NotBeNull();

        var falseValues = new[] { "false", "False", "FALSE", "0", "no", "off" };
        foreach (var value in falseValues)
        {
            var result = parseBooleanLikeValue!.Invoke(null, new object[] { value, "test" });
            result.Should().Be(false);
        }
    }

    [Fact]
    public void HasOption_WithPresentOption_ReturnsTrue()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var hasOption = programType.GetMethod("HasOption", BindingFlags.NonPublic | BindingFlags.Static);

        hasOption.Should().NotBeNull();

        var result = hasOption!.Invoke(null, new object[] { new[] { "--watch", "--theme", "dark" }, "--watch" });
        result.Should().Be(true);
    }

    [Fact]
    public void HasOption_WithMissingOption_ReturnsFalse()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var hasOption = programType.GetMethod("HasOption", BindingFlags.NonPublic | BindingFlags.Static);

        hasOption.Should().NotBeNull();

        var result = hasOption!.Invoke(null, new object[] { new[] { "--theme", "dark" }, "--watch" });
        result.Should().Be(false);
    }

    [Fact]
    public void HasOption_WithPowerModeHardwareVerify_ReturnsTrue()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var hasOption = programType.GetMethod("HasOption", BindingFlags.NonPublic | BindingFlags.Static);

        hasOption.Should().NotBeNull();

        var result = hasOption!.Invoke(
            null,
            new object[] { new[] { "--scenario", "power-mode", "--power-mode-hardware-verify" }, "--power-mode-hardware-verify" });

        result.Should().Be(true);
    }

    [Fact]
    public void TryReadOptionValue_WithPresentOption_ReturnsValue()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var tryReadOptionValue = programType.GetMethod("TryReadOptionValue", BindingFlags.NonPublic | BindingFlags.Static);

        tryReadOptionValue.Should().NotBeNull();

        var result = tryReadOptionValue!.Invoke(null, new object[] { new[] { "--plugin", "custom-mouse", "--theme", "dark" }, "--plugin" });
        result.Should().Be("custom-mouse");
    }

    [Fact]
    public void TryReadOptionValue_WithMissingOption_ReturnsNull()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var tryReadOptionValue = programType.GetMethod("TryReadOptionValue", BindingFlags.NonPublic | BindingFlags.Static);

        tryReadOptionValue.Should().NotBeNull();

        var result = tryReadOptionValue!.Invoke(null, new object[] { new[] { "--theme", "dark" }, "--plugin" });
        result.Should().BeNull();
    }

    [Fact]
    public void NormalizeRuntimeFixturePluginId_WithPluginPrefix_RemovesPrefix()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var normalizeRuntimeFixturePluginId = programType.GetMethod("NormalizeRuntimeFixturePluginId", BindingFlags.NonPublic | BindingFlags.Static);

        normalizeRuntimeFixturePluginId.Should().NotBeNull();

        var result = normalizeRuntimeFixturePluginId!.Invoke(null, new object[] { "UniversalDeviceToolkit.Plugins.CustomMouse" });
        result.Should().Be("custom-mouse");
    }

    [Fact]
    public void NormalizeRuntimeFixturePluginId_WithSimpleName_ReturnsNormalizedId()
    {
        var programType = Assembly.Load("MainAppPluginUi.Smoke").GetType("MainAppPluginUi.Smoke.Program", throwOnError: true)!;
        var normalizeRuntimeFixturePluginId = programType.GetMethod("NormalizeRuntimeFixturePluginId", BindingFlags.NonPublic | BindingFlags.Static);

        normalizeRuntimeFixturePluginId.Should().NotBeNull();

        var result = normalizeRuntimeFixturePluginId!.Invoke(null, new object[] { "ShellIntegration" });
        result.Should().Be("shell-integration");
    }

}
