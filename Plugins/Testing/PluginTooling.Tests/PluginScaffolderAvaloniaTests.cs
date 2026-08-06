using System.Reflection;
using PluginTooling.Core;
using Xunit;

namespace PluginTooling.Tests;

public sealed class PluginScaffolderAvaloniaTests
{
    [Fact]
    public void BuildProjectFile_IncludesAvaloniaAlongsideWpf()
    {
        var project = Invoke<string>(
            "BuildProjectFile",
            [typeof(ScaffoldRequest)],
            new ScaffoldRequest { FolderName = "SamplePlugin" });

        Assert.Contains("<UseWPF>true</UseWPF>", project, StringComparison.Ordinal);
        Assert.Contains("<PackageReference Include=\"Avalonia\" />", project, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPluginClass_FeatureSettingsAddsBothAvaloniaFactories()
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
            Name = "feature-settings",
            HasFeaturePage = true,
            HasSettingsPage = true
        };

        var source = Invoke<string>(
            "BuildPluginClass",
            [typeof(string), typeof(string), typeof(ScaffoldRequest), typeof(string), typeof(ArchetypeDefinition)],
            "SamplePlugin",
            "SamplePlugin",
            request,
            "Sample Plugin description",
            archetype);

        Assert.Contains("CreateAvaloniaPage() => new AvaloniaSamplePluginFeaturePage();", source, StringComparison.Ordinal);
        Assert.Contains("CreateAvaloniaPage() => new AvaloniaSamplePluginSettingsPage();", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPluginClass_SettingsOnlyDoesNotExposeFeatureFactory()
    {
        var request = new ScaffoldRequest
        {
            FolderName = "SettingsPlugin",
            PluginId = "settings-plugin",
            DisplayName = "Settings Plugin"
        };
        var archetype = new ArchetypeDefinition
        {
            Name = "settings-only",
            HasFeaturePage = false,
            HasSettingsPage = true
        };

        var source = Invoke<string>(
            "BuildPluginClass",
            [typeof(string), typeof(string), typeof(ScaffoldRequest), typeof(string), typeof(ArchetypeDefinition)],
            "SettingsPlugin",
            "SettingsPlugin",
            request,
            "Settings Plugin description",
            archetype);

        Assert.DoesNotContain("AvaloniaSettingsPluginFeaturePage", source, StringComparison.Ordinal);
        Assert.Contains("CreateAvaloniaPage() => new AvaloniaSettingsPluginSettingsPage();", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAvaloniaPages_UsesNativeLocalizedControls()
    {
        var source = Invoke<string>(
            "BuildAvaloniaPages",
            [typeof(string), typeof(string), typeof(ArchetypeDefinition)],
            "SamplePlugin",
            "SamplePlugin",
            new ArchetypeDefinition { HasFeaturePage = true, HasSettingsPage = true });

        Assert.Contains("using Avalonia.Controls;", source, StringComparison.Ordinal);
        Assert.Contains("public sealed class AvaloniaSamplePluginFeaturePage : UserControl", source, StringComparison.Ordinal);
        Assert.Contains("public sealed class AvaloniaSamplePluginSettingsPage : UserControl", source, StringComparison.Ordinal);
        Assert.Contains("SamplePluginText.FeaturePageTitle", source, StringComparison.Ordinal);
        Assert.Contains("SamplePluginText.SettingsPageDescription", source, StringComparison.Ordinal);
        Assert.Contains("TextWrapping = TextWrapping.Wrap", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Windows.Controls", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAvaloniaPages_SettingsOnlyOmitsFeaturePage()
    {
        var source = Invoke<string>(
            "BuildAvaloniaPages",
            [typeof(string), typeof(string), typeof(ArchetypeDefinition)],
            "SettingsPlugin",
            "SettingsPlugin",
            new ArchetypeDefinition { HasFeaturePage = false, HasSettingsPage = true });

        Assert.DoesNotContain("AvaloniaSettingsPluginFeaturePage", source, StringComparison.Ordinal);
        Assert.Contains("AvaloniaSettingsPluginSettingsPage", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAsync_WritesAvaloniaProjectPagesAndFactories()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), $"plugin-scaffold-avalonia-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(repositoryRoot, "Official"));
        Directory.CreateDirectory(Path.Combine(repositoryRoot, "Templates", "PluginArchetypes", "feature-settings"));
        File.WriteAllText(
            Path.Combine(repositoryRoot, "UniversalDeviceToolkit.Plugins.sln"),
            "Microsoft Visual Studio Solution File, Format Version 12.00\n" +
            "# Visual Studio Version 17\n" +
            "VisualStudioVersion = 17.0.31903.59\n" +
            "MinimumVisualStudioVersion = 10.0.40219.1\n" +
            "Global\n" +
            "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\n" +
            "\t\tDebug|Any CPU = Debug|Any CPU\n" +
            "\t\tRelease|Any CPU = Release|Any CPU\n" +
            "\tEndGlobalSection\n" +
            "\tGlobalSection(SolutionProperties) = preSolution\n" +
            "\t\tHideSolutionNode = FALSE\n" +
            "\tEndGlobalSection\n" +
            "EndGlobal\n");
        File.WriteAllText(
            Path.Combine(repositoryRoot, "Templates", "PluginArchetypes", "feature-settings", "template.json"),
            "{\"name\":\"feature-settings\",\"hasFeaturePage\":true,\"hasSettingsPage\":true,\"hasRuntime\":false,\"hasOptimizationCategory\":false}");

        try
        {
            var result = await new PluginScaffolder().CreateAsync(new ScaffoldRequest
            {
                RepositoryRoot = repositoryRoot,
                Template = PluginArchetype.FeatureSettings,
                FolderName = "GeneratedPlugin",
                PluginId = "generated-plugin",
                DisplayName = "Generated Plugin",
                Author = "Test"
            });

            var project = File.ReadAllText(result.ProjectPath);
            var pluginSource = File.ReadAllText(Path.Combine(result.PluginDirectory, "GeneratedPluginPlugin.cs"));
            var pagesPath = Path.Combine(result.PluginDirectory, "AvaloniaGeneratedPluginPages.cs");
            var pagesSource = File.ReadAllText(pagesPath);

            Assert.Contains("<PackageReference Include=\"Avalonia\" />", project, StringComparison.Ordinal);
            Assert.Contains("new AvaloniaGeneratedPluginFeaturePage()", pluginSource, StringComparison.Ordinal);
            Assert.Contains("new AvaloniaGeneratedPluginSettingsPage()", pluginSource, StringComparison.Ordinal);
            Assert.Contains("class AvaloniaGeneratedPluginFeaturePage", pagesSource, StringComparison.Ordinal);
            Assert.Contains("class AvaloniaGeneratedPluginSettingsPage", pagesSource, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(repositoryRoot))
            {
                Directory.Delete(repositoryRoot, recursive: true);
            }
        }
    }

    private static T Invoke<T>(string methodName, Type[] parameterTypes, params object?[] arguments)
    {
        var method = typeof(PluginScaffolder).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: parameterTypes,
            modifiers: null);

        Assert.NotNull(method);
        return (T)method!.Invoke(null, arguments)!;
    }
}
