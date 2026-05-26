using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class PluginIconResolverTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"llt-plugin-icon-resolver-{Guid.NewGuid():N}");

    public PluginIconResolverTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Resolve_ShouldPreferSymbolIcon()
    {
        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", "Rocket24", null, null);

        GetProperty(descriptor, "Kind").Should().Be("Symbol");
        GetProperty(descriptor, "Symbol").Should().Be("Rocket24");
        GetProperty(descriptor, "Monogram").Should().Be("DE");
    }

    [Fact]
    public void Resolve_ShouldLoadRelativeIconFromMetadataDirectory()
    {
        var pluginDirectory = Path.Combine(_root, "demo-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var metadataPath = Path.Combine(pluginDirectory, "plugin.json");
        var iconPath = Path.Combine(pluginDirectory, "icon.png");
        File.WriteAllText(metadataPath, "{}");
        File.WriteAllText(iconPath, "not-a-real-image");

        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", "icon.png", metadataPath, _root);

        GetProperty(descriptor, "Kind").Should().Be("Image");
        GetProperty(descriptor, "ImagePath").Should().Be(Path.GetFullPath(iconPath));
    }

    [Fact]
    public void Resolve_ShouldIgnorePathTraversalIconValue()
    {
        var pluginDirectory = Path.Combine(_root, "demo-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var metadataPath = Path.Combine(pluginDirectory, "plugin.json");
        var outsideIconPath = Path.Combine(_root, "outside.png");
        File.WriteAllText(metadataPath, "{}");
        File.WriteAllText(outsideIconPath, "not-a-real-image");

        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", "..\\outside.png", metadataPath, _root);

        GetProperty(descriptor, "Kind").Should().Be("Monogram");
        GetProperty(descriptor, "ImagePath").Should().BeNull();
    }

    [Fact]
    public void Resolve_ShouldFindStandardIconFileInPluginDirectory()
    {
        var pluginDirectory = Path.Combine(_root, "demo-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var iconPath = Path.Combine(pluginDirectory, "logo.ico");
        File.WriteAllText(iconPath, "not-a-real-image");

        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", null, null, _root);

        GetProperty(descriptor, "Kind").Should().Be("Image");
        GetProperty(descriptor, "ImagePath").Should().Be(Path.GetFullPath(iconPath));
    }

    [Fact]
    public void Resolve_ShouldIgnoreUnsafePluginIdForDirectoryLookup()
    {
        var escapedDirectory = Path.Combine(_root, "outside");
        Directory.CreateDirectory(escapedDirectory);
        var escapedIconPath = Path.Combine(escapedDirectory, "icon.png");
        File.WriteAllText(escapedIconPath, "not-a-real-image");

        var descriptor = InvokeResolve("../outside", "Demo Plugin", null, null, _root);

        GetProperty(descriptor, "Kind").Should().Be("Monogram");
        GetProperty(descriptor, "ImagePath").Should().BeNull();
    }

    [Fact]
    public void Resolve_ShouldIgnoreUrlIconValue()
    {
        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", "https://example.com/icon.png", null, null);

        // URL icon values should not be resolved as image paths
        GetProperty(descriptor, "Kind").Should().Be("Monogram");
        GetProperty(descriptor, "ImagePath").Should().BeNull();
    }

    [Fact]
    public void Resolve_ShouldLoadJpegIconFromPluginDirectory()
    {
        var pluginDirectory = Path.Combine(_root, "demo-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var iconPath = Path.Combine(pluginDirectory, "icon.jpeg");
        File.WriteAllText(iconPath, "not-a-real-image");

        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", null, null, _root);

        GetProperty(descriptor, "Kind").Should().Be("Image");
        GetProperty(descriptor, "ImagePath").Should().Be(Path.GetFullPath(iconPath));
    }

    [Fact]
    public void Resolve_ShouldLoadIcoIconFromPluginDirectory()
    {
        var pluginDirectory = Path.Combine(_root, "demo-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var iconPath = Path.Combine(pluginDirectory, "plugin.ico");
        File.WriteAllText(iconPath, "not-a-real-image");

        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", null, null, _root);

        GetProperty(descriptor, "Kind").Should().Be("Image");
        GetProperty(descriptor, "ImagePath").Should().Be(Path.GetFullPath(iconPath));
    }

    [Fact]
    public void Resolve_ShouldGenerateMonogramFromPluginName()
    {
        var descriptor = InvokeResolve("demo-plugin", "My Awesome Plugin", null, null, null);

        GetProperty(descriptor, "Kind").Should().Be("Monogram");
        GetProperty(descriptor, "Monogram").Should().Be("MA");
    }

    [Fact]
    public void Resolve_ShouldGenerateMonogramFromPluginIdWhenNameIsEmpty()
    {
        var descriptor = InvokeResolve("demo-plugin", "", null, null, null);

        GetProperty(descriptor, "Kind").Should().Be("Monogram");
        GetProperty(descriptor, "Monogram").Should().Be("DE");
    }

    [Fact]
    public void Resolve_ShouldGenerateMonogramFromPluginIdWhenNameIsNull()
    {
        var descriptor = InvokeResolve("demo-plugin", null!, null, null, null);

        GetProperty(descriptor, "Kind").Should().Be("Monogram");
        GetProperty(descriptor, "Monogram").Should().Be("DE");
    }

    [Fact]
    public void Resolve_ShouldFindIconInLocalSubdirectory()
    {
        var pluginDirectory = Path.Combine(_root, "local", "demo-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var iconPath = Path.Combine(pluginDirectory, "icon.png");
        File.WriteAllText(iconPath, "not-a-real-image");

        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", null, null, _root);

        GetProperty(descriptor, "Kind").Should().Be("Image");
        GetProperty(descriptor, "ImagePath").Should().Be(Path.GetFullPath(iconPath));
    }

    [Fact]
    public void Resolve_ShouldPreferMetadataDirectoryIconOverPluginsDirectory()
    {
        var pluginDirectory = Path.Combine(_root, "demo-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var metadataIconPath = Path.Combine(pluginDirectory, "icon.png");
        File.WriteAllText(metadataIconPath, "metadata-icon");

        var pluginsDirectory = Path.Combine(_root, "plugins", "demo-plugin");
        Directory.CreateDirectory(pluginsDirectory);
        var pluginsIconPath = Path.Combine(pluginsDirectory, "icon.png");
        File.WriteAllText(pluginsIconPath, "plugins-icon");

        var metadataPath = Path.Combine(pluginDirectory, "plugin.json");
        File.WriteAllText(metadataPath, "{}");

        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", null, metadataPath, Path.Combine(_root, "plugins"));

        GetProperty(descriptor, "Kind").Should().Be("Image");
        GetProperty(descriptor, "ImagePath").Should().Be(Path.GetFullPath(metadataIconPath));
    }

    [Fact]
    public void Resolve_ShouldHandlePluginIdWithHyphens()
    {
        var pluginDirectory = Path.Combine(_root, "my-awesome-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var iconPath = Path.Combine(pluginDirectory, "icon.png");
        File.WriteAllText(iconPath, "not-a-real-image");

        var descriptor = InvokeResolve("my-awesome-plugin", "My Awesome Plugin", null, null, _root);

        GetProperty(descriptor, "Kind").Should().Be("Image");
        GetProperty(descriptor, "ImagePath").Should().Be(Path.GetFullPath(iconPath));
    }

    [Fact]
    public void Resolve_ShouldFindIconByPluginIdAsFileName()
    {
        var pluginDirectory = Path.Combine(_root, "demo-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var iconPath = Path.Combine(pluginDirectory, "demo-plugin.png");
        File.WriteAllText(iconPath, "not-a-real-image");

        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", null, null, _root);

        GetProperty(descriptor, "Kind").Should().Be("Image");
        GetProperty(descriptor, "ImagePath").Should().Be(Path.GetFullPath(iconPath));
    }

    [Fact]
    public void Resolve_ShouldGenerateDefaultMonogramForNonLetterPluginName()
    {
        var descriptor = InvokeResolve("123-plugin", "123 Plugin", null, null, null);

        GetProperty(descriptor, "Kind").Should().Be("Monogram");
        GetProperty(descriptor, "Monogram").Should().Be("1P");
    }

    [Fact]
    public void Resolve_ShouldHandleAbsolutePathWithinAllowedDirectory()
    {
        var pluginDirectory = Path.Combine(_root, "demo-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var iconPath = Path.Combine(pluginDirectory, "custom-icon.png");
        File.WriteAllText(iconPath, "not-a-real-image");
        var metadataPath = Path.Combine(pluginDirectory, "plugin.json");
        File.WriteAllText(metadataPath, "{}");

        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", iconPath, metadataPath, _root);

        GetProperty(descriptor, "Kind").Should().Be("Image");
        GetProperty(descriptor, "ImagePath").Should().Be(Path.GetFullPath(iconPath));
    }

    [Fact]
    public void Resolve_ShouldIgnoreAbsolutePathOutsideAllowedDirectory()
    {
        var pluginDirectory = Path.Combine(_root, "demo-plugin");
        Directory.CreateDirectory(pluginDirectory);
        var outsideDirectory = Path.Combine(_root, "outside");
        Directory.CreateDirectory(outsideDirectory);
        var outsideIconPath = Path.Combine(outsideDirectory, "icon.png");
        File.WriteAllText(outsideIconPath, "not-a-real-image");
        var metadataPath = Path.Combine(pluginDirectory, "plugin.json");
        File.WriteAllText(metadataPath, "{}");

        var descriptor = InvokeResolve("demo-plugin", "Demo Plugin", outsideIconPath, metadataPath, _root);

        GetProperty(descriptor, "Kind").Should().Be("Monogram");
        GetProperty(descriptor, "ImagePath").Should().BeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static object InvokeResolve(
        string pluginId,
        string pluginName,
        string? iconValue,
        string? metadataFilePath,
        string? pluginsDirectory)
    {
        var resolverType = typeof(UniversalDeviceToolkit.WPF.Pages.PluginExtensionsPage).Assembly
            .GetType("UniversalDeviceToolkit.WPF.Utils.PluginIconResolver");
        var method = resolverType?.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static);

        method.Should().NotBeNull();

        return method!.Invoke(null, [pluginId, pluginName, iconValue, metadataFilePath, pluginsDirectory])
               ?? throw new InvalidOperationException("PluginIconResolver.Resolve returned null.");
    }

    private static string? GetProperty(object descriptor, string propertyName)
    {
        var value = descriptor.GetType().GetProperty(propertyName)?.GetValue(descriptor);
        return value?.ToString();
    }
}
