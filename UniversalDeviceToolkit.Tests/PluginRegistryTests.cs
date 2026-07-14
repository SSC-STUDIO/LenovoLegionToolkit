using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class PluginRegistryTests
{
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