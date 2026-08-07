using System.Reflection;
using System.Text.RegularExpressions;
using PluginTooling.Core;
using Xunit;

namespace PluginTooling.Tests;

public class PluginScaffolderChineseLocalizationTests
{
    private static readonly MethodInfo? BuildPluginClassMethod = typeof(PluginScaffolder).GetMethod(
        "BuildPluginClass",
        BindingFlags.Static | BindingFlags.NonPublic,
        binder: null,
        types: [typeof(string), typeof(string), typeof(ScaffoldRequest), typeof(string), typeof(ArchetypeDefinition)],
        modifiers: null);

    private static string InvokeBuildPluginClass(ScaffoldRequest request, ArchetypeDefinition archetype)
    {
        Assert.NotNull(BuildPluginClassMethod);
        return (string)BuildPluginClassMethod!.Invoke(
            null,
            ["SamplePlugin", "SamplePlugin", request, "Sample Plugin description", archetype])!;
    }

    /// <summary>
    /// The default scaffold (no --avalonia-only) must keep the full WPF + Avalonia
    /// surface: IPluginPage base, CreatePage factories and the Windows optimization
    /// category when the archetype declares one.
    /// </summary>
    [Fact]
    public void BuildPluginClass_DefaultKeepsWpfPageSurfaceAndOptimization()
    {
        var request = new ScaffoldRequest
        {
            FolderName = "SamplePlugin",
            PluginId = "sample-plugin",
            DisplayName = "Sample Plugin",
            Author = "Test",
            MinimumHostVersion = "5.0.0"
        };
        var archetype = new ArchetypeDefinition
        {
            Name = "runtime-optimization",
            HasFeaturePage = true,
            HasSettingsPage = true,
            HasRuntime = true,
            HasOptimizationCategory = true
        };

        var source = InvokeBuildPluginClass(request, archetype);

        Assert.Contains(": IPluginPage", source, StringComparison.Ordinal);
        Assert.Contains("public object CreatePage() => new SamplePluginControl();", source, StringComparison.Ordinal);
        Assert.Contains("public object CreatePage() => new SamplePluginSettingsControl();", source, StringComparison.Ordinal);
        Assert.Contains("WindowsOptimizationCategoryDefinition", source, StringComparison.Ordinal);
        Assert.Contains("CreateAvaloniaPage() => new AvaloniaSamplePluginFeaturePage();", source, StringComparison.Ordinal);
        Assert.Contains("CreateAvaloniaPage() => new AvaloniaSamplePluginSettingsPage();", source, StringComparison.Ordinal);
    }

    private static string InvokeBuildPluginChangelog(string displayName)
    {
        var method = typeof(PluginScaffolder).GetMethod(
            "BuildPluginChangelog",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            [typeof(string)],
            null);

        Assert.NotNull(method);
        return (string)method!.Invoke(null, [displayName])!;
    }

    private static string InvokeBuildResourceFile(
        string pluginName, string featureTitle,
        string settingsTitle, string featureDescription,
        string settingsDescription)
    {
        var method = typeof(PluginScaffolder).GetMethod(
            "BuildResourceFile",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            [typeof(string), typeof(string), typeof(string), typeof(string), typeof(string)],
            null);

        Assert.NotNull(method);
        return (string)method!.Invoke(null, [pluginName, featureTitle, settingsTitle, featureDescription, settingsDescription])!;
    }

    private static void InvokeWriteResxFiles(string pluginDirectory, string classPrefix, string displayName)
    {
        var method = typeof(PluginScaffolder).GetMethod(
            "WriteResxFiles",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            [typeof(string), typeof(string), typeof(string)],
            null);

        Assert.NotNull(method);
        method!.Invoke(null, [pluginDirectory, classPrefix, displayName]);
    }

    /// <summary>
    /// Regression test for mojibake in the scaffolded CHANGELOG.md.
    /// zh-Hans changelog text must be the correct UTF-8 string for
    /// "initial plugin scaffold", not the previously generated mojibake.
    /// </summary>
    [Fact]
    public void BuildPluginChangelog_ContainsCorrectChineseText()
    {
        var changelog = InvokeBuildPluginChangelog("TestPlugin");

        Assert.Contains("初始插件骨架", changelog);
        Assert.DoesNotContain("鍒濆鎻掍欢楠ㄦ灦", changelog);
        Assert.DoesNotContain("鍒濆", changelog);
    }

    /// <summary>
    /// Regression test for mojibake in the scaffolded zh-Hans .resx file.
    /// zh-Hans resource strings must be Settings / Feature preview / Settings preview,
    /// not the mojibake equivalents that were previously generated.
    /// </summary>
    [Fact]
    public void WriteResxFiles_ZhHansContainsCorrectChineseText()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"scaffold-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "Resources"));

        try
        {
            InvokeWriteResxFiles(tempDir, "Test", "TestPlugin");

            var zhHansPath = Path.Combine(tempDir, "Resources", "Resource.zh-Hans.resx");
            Assert.True(File.Exists(zhHansPath), "Resource.zh-Hans.resx was not generated");

            var zhHansContent = File.ReadAllText(zhHansPath);

            // Verify correct Chinese text is present
            Assert.Contains("设置", zhHansContent);
            Assert.Contains("功能预览", zhHansContent);
            Assert.Contains("设置预览", zhHansContent);

            // Verify mojibake is NOT present
            Assert.DoesNotContain("璁剧疆", zhHansContent);
            Assert.DoesNotContain("鍔熻兘", zhHansContent);
            Assert.DoesNotContain("璁剧疆棰勮", zhHansContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Verifies that the neutral and en resx files use English (not mojibake)
    /// and are unaffected by the fix.
    /// </summary>
    [Fact]
    public void WriteResxFiles_NeutralAndEnUseEnglishText()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"scaffold-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "Resources"));

        try
        {
            InvokeWriteResxFiles(tempDir, "Test", "TestPlugin");

            foreach (var fileName in new[] { "Resource.resx", "Resource.en.resx" })
            {
                var filePath = Path.Combine(tempDir, "Resources", fileName);
                Assert.True(File.Exists(filePath), $"{fileName} was not generated");

                var content = File.ReadAllText(filePath);
                Assert.Contains("TestPlugin Settings", content);
                Assert.Contains("feature preview", content);
                Assert.Contains("settings preview", content);

                // Ensure no mojibake leaked into neutral or English resources
                Assert.DoesNotContain("璁剧疆", content);
                Assert.DoesNotContain("鍔熻兘", content);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
