using System;
using System.Collections.Generic;
using System.IO;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Settings;
using Moq;

namespace LenovoLegionToolkit.Tests;

/// <summary>
/// Factory for creating common mocks used in tests
/// </summary>
public static class MockFactory
{
    /// <summary>
    /// Creates a mock file system watcher
    /// </summary>
    public static Mock<FileSystemWatcher> CreateFileSystemWatcher(string path = "C:\\Test")
    {
        var mock = new Mock<FileSystemWatcher>(path);
        mock.SetupAllProperties();
        return mock;
    }

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
    /// Creates a mock plugin manager
    /// </summary>
    public static Mock<IPluginManager> CreatePluginManager(
        IEnumerable<IPlugin>? plugins = null,
        bool isInstalled = true)
    {
        var mock = new Mock<IPluginManager>();
        var pluginList = plugins ?? new List<IPlugin>();

        mock.Setup(m => m.GetRegisteredPlugins()).Returns(pluginList);
        mock.Setup(m => m.IsInstalled(It.IsAny<string>())).Returns(isInstalled);
        mock.Setup(m => m.GetInstalledPluginIds()).Returns(
            pluginList.Select(p => p.Id));

        return mock;
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

    /// <summary>
    /// Creates mock application settings
    /// </summary>
    public static ApplicationSettings CreateMockApplicationSettings(
        bool extensionsEnabled = false,
        int pluginUpdateCheckFrequency = 24)
    {
        var store = new ApplicationSettings.ApplicationSettingsStore
        {
            ExtensionsEnabled = extensionsEnabled,
            CheckPluginUpdatesOnStartup = true,
            AutoDownloadPluginUpdates = false,
            NotifyOnPluginUpdate = true,
            PluginUpdateCheckFrequencyHours = pluginUpdateCheckFrequency,
            InstalledExtensions = new List<string>(),
            PendingPluginUpdates = new List<string>()
        };

        var mock = new Mock<ApplicationSettings>();
        mock.Setup(s => s.Store).Returns(store);

        return mock.Object;
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

    /// <summary>
    /// Builds an ApplicationSettingsStore with customizable properties
    /// </summary>
    public static SettingsStoreBuilder SettingsStore()
    {
        return new SettingsStoreBuilder();
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

public class SettingsStoreBuilder
{
    private bool _extensionsEnabled = false;
    private bool _checkPluginUpdatesOnStartup = true;
    private bool _autoDownloadPluginUpdates = false;
    private bool _notifyOnPluginUpdate = true;
    private int _pluginUpdateCheckFrequencyHours = 24;
    private List<string> _installedExtensions = new();
    private List<string> _pendingPluginUpdates = new();

    public SettingsStoreBuilder WithExtensionsEnabled(bool enabled)
    {
        _extensionsEnabled = enabled;
        return this;
    }

    public SettingsStoreBuilder WithCheckPluginUpdatesOnStartup(bool check)
    {
        _checkPluginUpdatesOnStartup = check;
        return this;
    }

    public SettingsStoreBuilder WithAutoDownloadPluginUpdates(bool autoDownload)
    {
        _autoDownloadPluginUpdates = autoDownload;
        return this;
    }

    public SettingsStoreBuilder WithNotifyOnPluginUpdate(bool notify)
    {
        _notifyOnPluginUpdate = notify;
        return this;
    }

    public SettingsStoreBuilder WithPluginUpdateCheckFrequency(int hours)
    {
        _pluginUpdateCheckFrequencyHours = hours;
        return this;
    }

    public SettingsStoreBuilder WithInstalledExtensions(params string[] extensions)
    {
        _installedExtensions = new List<string>(extensions);
        return this;
    }

    public SettingsStoreBuilder WithPendingUpdates(params string[] updates)
    {
        _pendingPluginUpdates = new List<string>(updates);
        return this;
    }

    public ApplicationSettings.ApplicationSettingsStore Build()
    {
        return new ApplicationSettings.ApplicationSettingsStore
        {
            ExtensionsEnabled = _extensionsEnabled,
            CheckPluginUpdatesOnStartup = _checkPluginUpdatesOnStartup,
            AutoDownloadPluginUpdates = _autoDownloadPluginUpdates,
            NotifyOnPluginUpdate = _notifyOnPluginUpdate,
            PluginUpdateCheckFrequencyHours = _pluginUpdateCheckFrequencyHours,
            InstalledExtensions = _installedExtensions,
            PendingPluginUpdates = _pendingPluginUpdates
        };
    }
}
