using System.Reflection;
using System.Text.RegularExpressions;
using PluginTooling.Core;
using Xunit;

namespace PluginTooling.Tests;

public class PluginScaffolderChineseLocalizationTests
{
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
    /// The Chinese text must be "初始插件骨架" (initial plugin scaffold),
    /// not the mojibake "鍒濆鎻掍欢楠ㄦ灦" that was previously generated.
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
    /// The Chinese strings must be "设置", "功能预览", "设置预览",
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
