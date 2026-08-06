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
