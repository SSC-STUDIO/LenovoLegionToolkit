using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginRegistryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeEmptyRegistry()
    {
        // Arrange & Act
        var registry = new PluginRegistry();

        // Assert
        registry.Count.Should().Be(0);
    }

    #endregion

    #region Register Tests

    [Fact]
    public void Register_ShouldAddPluginToRegistry()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        var metadata = CreateMetadata("test-plugin");

        // Act
        registry.Register(plugin, metadata);

        // Assert
        registry.Count.Should().Be(1);
        registry.IsRegistered("test-plugin").Should().BeTrue();
    }

    [Fact]
    public void Register_WithExistingPlugin_ShouldReplacePlugin()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin1 = CreateMockPlugin("test-plugin", "Test Plugin 1");
        var plugin2 = CreateMockPlugin("test-plugin", "Test Plugin 2");
        var metadata1 = CreateMetadata("test-plugin", "Plugin 1");
        var metadata2 = CreateMetadata("test-plugin", "Plugin 2");

        // Act
        registry.Register(plugin1, metadata1);
        registry.Register(plugin2, metadata2);

        // Assert
        registry.Count.Should().Be(1);
        var retrievedPlugin = registry.Get("test-plugin");
        retrievedPlugin.Should().NotBeNull();
        retrievedPlugin!.Name.Should().Be("Test Plugin 2");
    }

    [Fact]
    public void Register_WithNullPluginId_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin(null!, "Test Plugin");
        var metadata = CreateMetadata("test-plugin");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.Register(plugin, metadata));
    }

    [Fact]
    public void Register_WithEmptyPluginId_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("", "Test Plugin");
        var metadata = CreateMetadata("test-plugin");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.Register(plugin, metadata));
    }

    [Fact]
    public void Register_WithStartedExistingPlugin_ShouldStopExistingPlugin()
    {
        // Arrange
        var registry = new PluginRegistry();
        var mockPlugin1 = new Mock<IPlugin>();
        mockPlugin1.Setup(p => p.Id).Returns("test-plugin");
        mockPlugin1.Setup(p => p.Name).Returns("Test Plugin 1");
        mockPlugin1.Setup(p => p.Stop());

        var mockPlugin2 = new Mock<IPlugin>();
        mockPlugin2.Setup(p => p.Id).Returns("test-plugin");
        mockPlugin2.Setup(p => p.Name).Returns("Test Plugin 2");

        var metadata1 = CreateMetadata("test-plugin");
        var metadata2 = CreateMetadata("test-plugin");

        // Act
        registry.Register(mockPlugin1.Object, metadata1);
        registry.MarkStarted("test-plugin");
        registry.Register(mockPlugin2.Object, metadata2);

        // Assert
        mockPlugin1.Verify(p => p.Stop(), Times.Once);
    }

    [Fact]
    public void Register_MultiplePlugins_ShouldRegisterAll()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugins = new[]
        {
            (Plugin: CreateMockPlugin("plugin-1", "Plugin 1"), Metadata: CreateMetadata("plugin-1")),
            (Plugin: CreateMockPlugin("plugin-2", "Plugin 2"), Metadata: CreateMetadata("plugin-2")),
            (Plugin: CreateMockPlugin("plugin-3", "Plugin 3"), Metadata: CreateMetadata("plugin-3"))
        };

        // Act
        foreach (var (plugin, metadata) in plugins)
        {
            registry.Register(plugin, metadata);
        }

        // Assert
        registry.Count.Should().Be(3);
        registry.IsRegistered("plugin-1").Should().BeTrue();
        registry.IsRegistered("plugin-2").Should().BeTrue();
        registry.IsRegistered("plugin-3").Should().BeTrue();
    }

    #endregion

    #region Unregister Tests

    [Fact]
    public void Unregister_ShouldRemovePluginFromRegistry()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        var metadata = CreateMetadata("test-plugin");
        registry.Register(plugin, metadata);

        // Act
        registry.Unregister("test-plugin");

        // Assert
        registry.Count.Should().Be(0);
        registry.IsRegistered("test-plugin").Should().BeFalse();
    }

    [Fact]
    public void Unregister_WithNullPluginId_ShouldNotThrow()
    {
        // Arrange
        var registry = new PluginRegistry();

        // Act & Assert - Should not throw
        registry.Unregister(null!);
    }

    [Fact]
    public void Unregister_WithEmptyPluginId_ShouldNotThrow()
    {
        // Arrange
        var registry = new PluginRegistry();

        // Act & Assert - Should not throw
        registry.Unregister("");
    }

    [Fact]
    public void Unregister_WithNonExistentPlugin_ShouldNotThrow()
    {
        // Arrange
        var registry = new PluginRegistry();

        // Act & Assert - Should not throw
        registry.Unregister("non-existent-plugin");
    }

    [Fact]
    public void Unregister_ShouldStopPluginAndCallOnUninstalled()
    {
        // Arrange
        var registry = new PluginRegistry();
        var mockPlugin = new Mock<IPlugin>();
        mockPlugin.Setup(p => p.Id).Returns("test-plugin");
        mockPlugin.Setup(p => p.Name).Returns("Test Plugin");
        mockPlugin.Setup(p => p.Stop());
        mockPlugin.Setup(p => p.OnUninstalled());

        var metadata = CreateMetadata("test-plugin");
        registry.Register(mockPlugin.Object, metadata);

        // Act
        registry.Unregister("test-plugin");

        // Assert
        mockPlugin.Verify(p => p.Stop(), Times.Once);
        mockPlugin.Verify(p => p.OnUninstalled(), Times.Once);
    }

    [Fact]
    public void Unregister_ShouldRemoveFromStartedPlugins()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        var metadata = CreateMetadata("test-plugin");
        registry.Register(plugin, metadata);
        registry.MarkStarted("test-plugin");

        // Act
        registry.Unregister("test-plugin");

        // Assert
        registry.GetStartedPluginIds().Should().NotContain("test-plugin");
    }

    #endregion

    #region Get Tests

    [Fact]
    public void Get_ShouldReturnRegisteredPlugin()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        var metadata = CreateMetadata("test-plugin");
        registry.Register(plugin, metadata);

        // Act
        var result = registry.Get("test-plugin");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-plugin");
    }

    [Fact]
    public void Get_WithNonExistentPlugin_ShouldReturnNull()
    {
        // Arrange
        var registry = new PluginRegistry();

        // Act
        var result = registry.Get("non-existent-plugin");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Get_ShouldBeCaseInsensitive()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("Test-Plugin", "Test Plugin");
        var metadata = CreateMetadata("Test-Plugin");
        registry.Register(plugin, metadata);

        // Act
        var result = registry.Get("test-plugin");

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public void GetAll_ShouldReturnAllRegisteredPlugins()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugins = new[]
        {
            (Plugin: CreateMockPlugin("plugin-1", "Plugin 1"), Metadata: CreateMetadata("plugin-1")),
            (Plugin: CreateMockPlugin("plugin-2", "Plugin 2"), Metadata: CreateMetadata("plugin-2"))
        };

        foreach (var (plugin, metadata) in plugins)
        {
            registry.Register(plugin, metadata);
        }

        // Act
        var result = registry.GetAll().ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.Id).Should().Contain(new[] { "plugin-1", "plugin-2" });
    }

    [Fact]
    public void GetAll_WhenEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        var registry = new PluginRegistry();

        // Act
        var result = registry.GetAll();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetMetadata Tests

    [Fact]
    public void GetMetadata_ShouldReturnPluginMetadata()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        var metadata = CreateMetadata("test-plugin", "Test Plugin", "1.0.0", "Test Author");
        registry.Register(plugin, metadata);

        // Act
        var result = registry.GetMetadata("test-plugin");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-plugin");
        result.Name.Should().Be("Test Plugin");
        result.Version.Should().Be("1.0.0");
        result.Author.Should().Be("Test Author");
    }

    [Fact]
    public void GetMetadata_WithNonExistentPlugin_ShouldReturnNull()
    {
        // Arrange
        var registry = new PluginRegistry();

        // Act
        var result = registry.GetMetadata("non-existent-plugin");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsRegistered Tests

    [Fact]
    public void IsRegistered_WhenPluginExists_ShouldReturnTrue()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        var metadata = CreateMetadata("test-plugin");
        registry.Register(plugin, metadata);

        // Act
        var result = registry.IsRegistered("test-plugin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_WhenPluginDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var registry = new PluginRegistry();

        // Act
        var result = registry.IsRegistered("non-existent-plugin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRegistered_ShouldBeCaseInsensitive()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("Test-Plugin", "Test Plugin");
        var metadata = CreateMetadata("Test-Plugin");
        registry.Register(plugin, metadata);

        // Act
        var result = registry.IsRegistered("test-plugin");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region MarkStarted/MarkStopped Tests

    [Fact]
    public void MarkStarted_ShouldAddPluginToStartedSet()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        var metadata = CreateMetadata("test-plugin");
        registry.Register(plugin, metadata);

        // Act
        var result = registry.MarkStarted("test-plugin");

        // Assert
        result.Should().BeTrue();
        registry.GetStartedPluginIds().Should().Contain("test-plugin");
    }

    [Fact]
    public void MarkStarted_WhenAlreadyStarted_ShouldReturnFalse()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        var metadata = CreateMetadata("test-plugin");
        registry.Register(plugin, metadata);
        registry.MarkStarted("test-plugin");

        // Act
        var result = registry.MarkStarted("test-plugin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MarkStopped_ShouldRemovePluginFromStartedSet()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        var metadata = CreateMetadata("test-plugin");
        registry.Register(plugin, metadata);
        registry.MarkStarted("test-plugin");

        // Act
        registry.MarkStopped("test-plugin");

        // Assert
        registry.GetStartedPluginIds().Should().NotContain("test-plugin");
    }

    [Fact]
    public void MarkStopped_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        var metadata = CreateMetadata("test-plugin");
        registry.Register(plugin, metadata);

        // Act & Assert - Should not throw
        registry.MarkStopped("test-plugin");
    }

    [Fact]
    public void ReplaceWithMetadataAdapter_ShouldSwapLivePluginForAdapter()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        registry.Register(plugin, CreateMetadata("test-plugin", name: "Metadata Name"));
        registry.MarkStarted("test-plugin");

        // Act
        var replaced = registry.ReplaceWithMetadataAdapter("test-plugin");
        var stored = registry.Get("test-plugin");

        // Assert
        replaced.Should().BeTrue();
        stored.Should().BeOfType<PluginManifestAdapter>();
        stored!.Name.Should().Be("Metadata Name");
        registry.GetStartedPluginIds().Should().NotContain("test-plugin");
    }

    #endregion

    #region GetStartedPluginIds Tests

    [Fact]
    public void GetStartedPluginIds_ShouldReturnStartedPlugins()
    {
        // Arrange
        var registry = new PluginRegistry();

        var plugin1 = CreateMockPlugin("plugin-1", "Plugin 1");
        var plugin2 = CreateMockPlugin("plugin-2", "Plugin 2");
        var plugin3 = CreateMockPlugin("plugin-3", "Plugin 3");

        registry.Register(plugin1, CreateMetadata("plugin-1"));
        registry.Register(plugin2, CreateMetadata("plugin-2"));
        registry.Register(plugin3, CreateMetadata("plugin-3"));

        registry.MarkStarted("plugin-1");
        registry.MarkStarted("plugin-3");

        // Act
        var result = registry.GetStartedPluginIds().ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(new[] { "plugin-1", "plugin-3" });
        result.Should().NotContain("plugin-2");
    }

    [Fact]
    public void GetStartedPluginIds_WhenNoneStarted_ShouldReturnEmpty()
    {
        // Arrange
        var registry = new PluginRegistry();
        var plugin = CreateMockPlugin("test-plugin", "Test Plugin");
        registry.Register(plugin, CreateMetadata("test-plugin"));

        // Act
        var result = registry.GetStartedPluginIds();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldRemoveAllPlugins()
    {
        // Arrange
        var registry = new PluginRegistry();
        registry.Register(CreateMockPlugin("plugin-1", "Plugin 1"), CreateMetadata("plugin-1"));
        registry.Register(CreateMockPlugin("plugin-2", "Plugin 2"), CreateMetadata("plugin-2"));
        registry.MarkStarted("plugin-1");

        // Act
        registry.Clear();

        // Assert
        registry.Count.Should().Be(0);
        registry.GetStartedPluginIds().Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldCallOnUninstalledForAllPlugins()
    {
        // Arrange
        var registry = new PluginRegistry();
        var mockPlugin1 = new Mock<IPlugin>();
        mockPlugin1.Setup(p => p.Id).Returns("plugin-1");
        mockPlugin1.Setup(p => p.OnUninstalled());

        var mockPlugin2 = new Mock<IPlugin>();
        mockPlugin2.Setup(p => p.Id).Returns("plugin-2");
        mockPlugin2.Setup(p => p.OnUninstalled());

        registry.Register(mockPlugin1.Object, CreateMetadata("plugin-1"));
        registry.Register(mockPlugin2.Object, CreateMetadata("plugin-2"));

        // Act
        registry.Clear();

        // Assert
        mockPlugin1.Verify(p => p.OnUninstalled(), Times.Once);
        mockPlugin2.Verify(p => p.OnUninstalled(), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static IPlugin CreateMockPlugin(string id, string name)
    {
        var mock = new Mock<IPlugin>();
        mock.Setup(p => p.Id).Returns(id);
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.Description).Returns($"Description for {name}");
        mock.Setup(p => p.Icon).Returns("Apps24");
        mock.Setup(p => p.IsSystemPlugin).Returns(false);
        mock.Setup(p => p.Dependencies).Returns(Array.Empty<string>());
        return mock.Object;
    }

    private static PluginMetadata CreateMetadata(
        string id,
        string name = "Test Plugin",
        string version = "1.0.0",
        string? author = null)
    {
        return new PluginMetadata
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            Icon = "Apps24",
            Version = version,
            Author = author,
            MinimumHostVersion = "1.0.0"
        };
    }

    #endregion

    #region Register / Unregister Tests

    [Fact]
    public void Register_NewPlugin_ShouldAddToRegistry()
    {
        var registry = new PluginRegistry();
        var plugin = new PluginManifestAdapter(new PluginManifest { Id = "p1", Name = "Plugin 1" });
        var metadata = new PluginMetadata { Id = "p1", Name = "Plugin 1" };

        registry.Register(plugin, metadata);

        registry.Count.Should().Be(1);
        registry.IsRegistered("p1").Should().BeTrue();
    }

    [Fact]
    public void Register_DuplicatePlugin_ShouldReplaceExisting()
    {
        var registry = new PluginRegistry();
        var plugin1 = new PluginManifestAdapter(new PluginManifest { Id = "p1", Name = "Plugin 1 v1" });
        var plugin2 = new PluginManifestAdapter(new PluginManifest { Id = "p1", Name = "Plugin 1 v2" });
        var metadata = new PluginMetadata { Id = "p1", Name = "Plugin 1" };

        registry.Register(plugin1, metadata);
        registry.Register(plugin2, metadata);

        registry.Count.Should().Be(1);
        var retrieved = registry.Get("p1");
        retrieved.Should().BeSameAs(plugin2);
    }

    [Fact]
    public void Register_EmptyId_ShouldThrow()
    {
        var registry = new PluginRegistry();
        var plugin = new PluginManifestAdapter(new PluginManifest { Id = "" });
        var metadata = new PluginMetadata { Id = "" };

        var act = () => registry.Register(plugin, metadata);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Unregister_ExistingPlugin_ShouldRemove()
    {
        var registry = new PluginRegistry();
        var plugin = new PluginManifestAdapter(new PluginManifest { Id = "p1" });
        registry.Register(plugin, new PluginMetadata { Id = "p1" });

        registry.Unregister("p1");

        registry.Count.Should().Be(0);
        registry.IsRegistered("p1").Should().BeFalse();
    }

    [Fact]
    public void Unregister_NonExistent_ShouldNotThrow()
    {
        var registry = new PluginRegistry();
        var act = () => registry.Unregister("nonexistent");
        act.Should().NotThrow();
    }

    [Fact]
    public void Unregister_Null_ShouldNotThrow()
    {
        var registry = new PluginRegistry();
        var act = () => registry.Unregister(null!);
        act.Should().NotThrow();
    }

    [Fact]
    public void Unregister_Empty_ShouldNotThrow()
    {
        var registry = new PluginRegistry();
        var act = () => registry.Unregister("");
        act.Should().NotThrow();
    }

    #endregion

    #region Get Tests

    [Fact]
    public void Get_ExistingPlugin_ShouldReturnPlugin()
    {
        var registry = new PluginRegistry();
        var plugin = new PluginManifestAdapter(new PluginManifest { Id = "p1" });
        registry.Register(plugin, new PluginMetadata { Id = "p1" });

        registry.Get("p1").Should().BeSameAs(plugin);
    }

    [Fact]
    public void Get_NonExistent_ShouldReturnNull()
    {
        var registry = new PluginRegistry();
        registry.Get("nonexistent").Should().BeNull();
    }

    [Fact]
    public void Get_CaseInsensitive_ShouldMatch()
    {
        var registry = new PluginRegistry();
        var plugin = new PluginManifestAdapter(new PluginManifest { Id = "MyPlugin" });
        registry.Register(plugin, new PluginMetadata { Id = "MyPlugin" });

        registry.Get("myplugin").Should().BeSameAs(plugin);
        registry.Get("MYPLUGIN").Should().BeSameAs(plugin);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public void GetAll_Empty_ShouldReturnEmpty()
    {
        var registry = new PluginRegistry();
        registry.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void GetAll_Multiple_ShouldReturnAll()
    {
        var registry = new PluginRegistry();
        registry.Register(new PluginManifestAdapter(new PluginManifest { Id = "p1" }), new PluginMetadata { Id = "p1" });
        registry.Register(new PluginManifestAdapter(new PluginManifest { Id = "p2" }), new PluginMetadata { Id = "p2" });

        registry.GetAll().Should().HaveCount(2);
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public void GetMetadata_Existing_ShouldReturnMetadata()
    {
        var registry = new PluginRegistry();
        var metadata = new PluginMetadata { Id = "p1", Name = "Plugin 1", Author = "Test Author" };
        registry.Register(new PluginManifestAdapter(new PluginManifest { Id = "p1" }), metadata);

        var result = registry.GetMetadata("p1");
        result.Should().NotBeNull();
        result!.Author.Should().Be("Test Author");
    }

    [Fact]
    public void GetMetadata_NonExistent_ShouldReturnNull()
    {
        var registry = new PluginRegistry();
        registry.GetMetadata("nonexistent").Should().BeNull();
    }

    [Fact]
    public void GetAllMetadata_ShouldReturnAll()
    {
        var registry = new PluginRegistry();
        registry.Register(new PluginManifestAdapter(new PluginManifest { Id = "p1" }), new PluginMetadata { Id = "p1" });
        registry.Register(new PluginManifestAdapter(new PluginManifest { Id = "p2" }), new PluginMetadata { Id = "p2" });

        registry.GetAllMetadata().Should().HaveCount(2);
    }

    #endregion

    #region IsRegistered / IsStarted Tests

    [Fact]
    public void IsRegistered_Null_ShouldReturnFalse()
    {
        var registry = new PluginRegistry();
        registry.IsRegistered(null!).Should().BeFalse();
    }

    [Fact]
    public void IsRegistered_Empty_ShouldReturnFalse()
    {
        var registry = new PluginRegistry();
        registry.IsRegistered("").Should().BeFalse();
    }

    [Fact]
    public void IsStarted_Null_ShouldReturnFalse()
    {
        var registry = new PluginRegistry();
        registry.IsStarted(null!).Should().BeFalse();
    }

    [Fact]
    public void IsStarted_Empty_ShouldReturnFalse()
    {
        var registry = new PluginRegistry();
        registry.IsStarted("").Should().BeFalse();
    }

    #endregion

    #region MarkStarted / MarkStopped Tests

    [Fact]
    public void MarkStarted_NewPlugin_ShouldReturnTrue()
    {
        var registry = new PluginRegistry();
        registry.MarkStarted("p1").Should().BeTrue();
    }

    [Fact]
    public void MarkStarted_AlreadyStarted_ShouldReturnFalse()
    {
        var registry = new PluginRegistry();
        registry.MarkStarted("p1").Should().BeTrue();
        registry.MarkStarted("p1").Should().BeFalse();
    }

    [Fact]
    public void MarkStopped_StartedPlugin_ShouldStop()
    {
        var registry = new PluginRegistry();
        registry.MarkStarted("p1");
        registry.MarkStopped("p1");
        registry.IsStarted("p1").Should().BeFalse();
    }

    [Fact]
    public void MarkStopped_NotStarted_ShouldNotThrow()
    {
        var registry = new PluginRegistry();
        var act = () => registry.MarkStopped("p1");
        act.Should().NotThrow();
    }

    [Fact]
    public void GetStartedPluginIds_ShouldReturnAll()
    {
        var registry = new PluginRegistry();
        registry.MarkStarted("p1");
        registry.MarkStarted("p2");

        registry.GetStartedPluginIds().Should().Contain(new[] { "p1", "p2" });
    }

    [Fact]
    public void GetStartedPluginIds_Empty_ShouldReturnEmpty()
    {
        var registry = new PluginRegistry();
        registry.GetStartedPluginIds().Should().BeEmpty();
    }

    #endregion

    #region GetByAuthor Tests

    [Fact]
    public void GetByAuthor_MatchingAuthor_ShouldReturnPlugins()
    {
        var registry = new PluginRegistry();
        registry.Register(
            new PluginManifestAdapter(new PluginManifest { Id = "p1" }),
            new PluginMetadata { Id = "p1", Author = "TestAuthor" });
        registry.Register(
            new PluginManifestAdapter(new PluginManifest { Id = "p2" }),
            new PluginMetadata { Id = "p2", Author = "OtherAuthor" });

        var results = registry.GetByAuthor("TestAuthor").ToList();
        results.Should().HaveCount(1);
        results[0].Id.Should().Be("p1");
    }

    [Fact]
    public void GetByAuthor_CaseInsensitive_ShouldMatch()
    {
        var registry = new PluginRegistry();
        registry.Register(
            new PluginManifestAdapter(new PluginManifest { Id = "p1" }),
            new PluginMetadata { Id = "p1", Author = "TestAuthor" });

        registry.GetByAuthor("testauthor").Should().HaveCount(1);
    }

    [Fact]
    public void GetByAuthor_Null_ShouldReturnEmpty()
    {
        var registry = new PluginRegistry();
        registry.GetByAuthor(null!).Should().BeEmpty();
    }

    [Fact]
    public void GetByAuthor_Empty_ShouldReturnEmpty()
    {
        var registry = new PluginRegistry();
        registry.GetByAuthor("").Should().BeEmpty();
    }

    [Fact]
    public void GetByAuthor_NoMatch_ShouldReturnEmpty()
    {
        var registry = new PluginRegistry();
        registry.Register(
            new PluginManifestAdapter(new PluginManifest { Id = "p1" }),
            new PluginMetadata { Id = "p1", Author = "Author1" });

        registry.GetByAuthor("Author2").Should().BeEmpty();
    }

    #endregion

    #region ReplaceWithMetadataAdapter Tests

    [Fact]
    public void ReplaceWithMetadataAdapter_Existing_ShouldReturnTrue()
    {
        var registry = new PluginRegistry();
        var plugin = new PluginManifestAdapter(new PluginManifest { Id = "p1", Name = "P1" });
        registry.Register(plugin, new PluginMetadata { Id = "p1", Name = "P1" });
        registry.MarkStarted("p1");

        var result = registry.ReplaceWithMetadataAdapter("p1");

        result.Should().BeTrue();
        registry.IsStarted("p1").Should().BeFalse();
    }

    [Fact]
    public void ReplaceWithMetadataAdapter_NonExistent_ShouldReturnFalse()
    {
        var registry = new PluginRegistry();
        registry.ReplaceWithMetadataAdapter("nonexistent").Should().BeFalse();
    }

    [Fact]
    public void ReplaceWithMetadataAdapter_Null_ShouldReturnFalse()
    {
        var registry = new PluginRegistry();
        registry.ReplaceWithMetadataAdapter(null!).Should().BeFalse();
    }

    [Fact]
    public void ReplaceWithMetadataAdapter_Empty_ShouldReturnFalse()
    {
        var registry = new PluginRegistry();
        registry.ReplaceWithMetadataAdapter("").Should().BeFalse();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldRemoveAll()
    {
        var registry = new PluginRegistry();
        registry.Register(new PluginManifestAdapter(new PluginManifest { Id = "p1" }), new PluginMetadata { Id = "p1" });
        registry.Register(new PluginManifestAdapter(new PluginManifest { Id = "p2" }), new PluginMetadata { Id = "p2" });
        registry.MarkStarted("p1");

        registry.Clear();

        registry.Count.Should().Be(0);
        registry.GetAll().Should().BeEmpty();
        registry.GetAllMetadata().Should().BeEmpty();
    }

    [Fact]
    public void Clear_Empty_ShouldNotThrow()
    {
        var registry = new PluginRegistry();
        var act = () => registry.Clear();
        act.Should().NotThrow();
    }

    #endregion

    #region Unregister With Started Plugin Tests

    [Fact]
    public void Unregister_StartedPlugin_ShouldStopAndRemove()
    {
        var registry = new PluginRegistry();
        registry.Register(new PluginManifestAdapter(new PluginManifest { Id = "p1" }), new PluginMetadata { Id = "p1" });
        registry.MarkStarted("p1");

        registry.Unregister("p1");

        registry.IsRegistered("p1").Should().BeFalse();
        registry.IsStarted("p1").Should().BeFalse();
    }

    #endregion
}
