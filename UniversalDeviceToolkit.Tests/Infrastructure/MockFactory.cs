using System;
using LenovoLegionToolkit.Lib.Plugins;
using Moq;

namespace UniversalDeviceToolkit.Tests;

/// <summary>
/// Factory for creating common mocks used in tests
/// </summary>
public static class MockFactory
{
    /// <summary>
    /// Creates a mock plugin
    /// </summary>
    public static IPlugin CreateMockPlugin(
        string? id = null,
        string? name = null,
        string? description = null,
        string? icon = null,
        bool isSystemPlugin = false,
        string[]? dependencies = null)
    {
        var mock = new Mock<IPlugin>();
        mock.Setup(p => p.Id).Returns(id ?? "TestPlugin");
        mock.Setup(p => p.Name).Returns(name ?? "Test Plugin");
        mock.Setup(p => p.Description).Returns(description ?? "Test description");
        mock.Setup(p => p.Icon).Returns(icon ?? "Apps24");
        mock.Setup(p => p.IsSystemPlugin).Returns(isSystemPlugin);
        mock.Setup(p => p.Dependencies).Returns(dependencies ?? Array.Empty<string>());
        return mock.Object;
    }

    /// <summary>
    /// Creates mock plugin metadata
    /// </summary>
    public static PluginMetadata CreateMockPluginMetadata(
        string? id = null,
        string? version = null,
        string? minimumHostVersion = null)
    {
        return new PluginMetadata
        {
            Id = id ?? "TestPlugin",
            Name = "Test Plugin",
            Description = "Test description",
            Icon = "Apps24",
            IsSystemPlugin = false,
            Version = version ?? "1.0.0",
            MinimumHostVersion = minimumHostVersion ?? "1.0.0",
            Author = "Test Author"
        };
    }

    /// <summary>
    /// Creates mock plugin manifest
    /// </summary>
    public static PluginManifest CreateMockPluginManifest(
        string? id = null,
        string? version = null,
        string? minimumHostVersion = null,
        string? downloadUrl = null)
    {
        return new PluginManifest
        {
            Id = id ?? "TestPlugin",
            Name = "Test Plugin",
            Description = "Test description",
            Version = version ?? "1.0.0",
            MinimumHostVersion = minimumHostVersion ?? "1.0.0",
            DownloadUrl = downloadUrl ?? "https://example.com/plugin.zip",
            Author = "Test Author",
            IsSystemPlugin = false,
            FileSize = 1024,
            ReleaseDate = DateTime.UtcNow.ToString("o")
        };
    }
}

/// <summary>
/// Test data builders for creating complex test objects
/// </summary>
public static class Builder
{
    /// <summary>
    /// Builds a PluginManifest with customizable properties
    /// </summary>
    public static PluginManifestBuilder PluginManifest()
    {
        return new PluginManifestBuilder();
    }

    /// <summary>
    /// Builds a PluginMetadata with customizable properties
    /// </summary>
    public static PluginMetadataBuilder PluginMetadata()
    {
        return new PluginMetadataBuilder();
    }
}

public class PluginManifestBuilder
{
    private string _id = "TestPlugin";
    private string _name = "Test Plugin";
    private string _description = "Test description";
    private string _version = "1.0.0";
    private string _minimumHostVersion = "1.0.0";
    private string _downloadUrl = "https://example.com/plugin.zip";
    private string _author = "Test Author";
    private bool _isSystemPlugin = false;
    private long _fileSize = 1024;
    private string[]? _tags = null;
    private string[]? _dependencies = null;

    public PluginManifestBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public PluginManifestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PluginManifestBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public PluginManifestBuilder WithVersion(string version)
    {
        _version = version;
        return this;
    }

    public PluginManifestBuilder WithMinimumHostVersion(string version)
    {
        _minimumHostVersion = version;
        return this;
    }

    public PluginManifestBuilder WithDownloadUrl(string url)
    {
        _downloadUrl = url;
        return this;
    }

    public PluginManifestBuilder WithAuthor(string author)
    {
        _author = author;
        return this;
    }

    public PluginManifestBuilder AsSystemPlugin(bool isSystem = true)
    {
        _isSystemPlugin = isSystem;
        return this;
    }

    public PluginManifestBuilder WithFileSize(long size)
    {
        _fileSize = size;
        return this;
    }

    public PluginManifestBuilder WithTags(params string[] tags)
    {
        _tags = tags;
        return this;
    }

    public PluginManifestBuilder WithDependencies(params string[] dependencies)
    {
        _dependencies = dependencies;
        return this;
    }

    public PluginManifest Build()
    {
        return new PluginManifest
        {
            Id = _id,
            Name = _name,
            Description = _description,
            Version = _version,
            MinimumHostVersion = _minimumHostVersion,
            DownloadUrl = _downloadUrl,
            Author = _author,
            IsSystemPlugin = _isSystemPlugin,
            FileSize = _fileSize,
            Tags = _tags,
            Dependencies = _dependencies,
            ReleaseDate = DateTime.UtcNow.ToString("o")
        };
    }
}

public class PluginMetadataBuilder
{
    private string _id = "TestPlugin";
    private string _name = "Test Plugin";
    private string _description = "Test description";
    private string _icon = "Apps24";
    private bool _isSystemPlugin = false;
    private string _version = "1.0.0";
    private string _minimumHostVersion = "1.0.0";
    private string _author = "Test Author";
    private string[]? _dependencies = null;
    private string? _filePath = null;

    public PluginMetadataBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public PluginMetadataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PluginMetadataBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public PluginMetadataBuilder WithIcon(string icon)
    {
        _icon = icon;
        return this;
    }

    public PluginMetadataBuilder AsSystemPlugin(bool isSystem = true)
    {
        _isSystemPlugin = isSystem;
        return this;
    }

    public PluginMetadataBuilder WithVersion(string version)
    {
        _version = version;
        return this;
    }

    public PluginMetadataBuilder WithMinimumHostVersion(string version)
    {
        _minimumHostVersion = version;
        return this;
    }

    public PluginMetadataBuilder WithAuthor(string author)
    {
        _author = author;
        return this;
    }

    public PluginMetadataBuilder WithDependencies(params string[] dependencies)
    {
        _dependencies = dependencies;
        return this;
    }

    public PluginMetadataBuilder WithFilePath(string path)
    {
        _filePath = path;
        return this;
    }

    public PluginMetadata Build()
    {
        return new PluginMetadata
        {
            Id = _id,
            Name = _name,
            Description = _description,
            Icon = _icon,
            IsSystemPlugin = _isSystemPlugin,
            Version = _version,
            MinimumHostVersion = _minimumHostVersion,
            Author = _author,
            Dependencies = _dependencies,
            FilePath = _filePath
        };
    }
}
