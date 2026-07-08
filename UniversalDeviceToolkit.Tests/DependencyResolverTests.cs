using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class DependencyResolverTests
{
    private readonly DependencyResolver _resolver = new();

    #region PluginDependency Defaults Tests

    [Fact]
    public void PluginDependency_Defaults_ShouldHaveExpectedValues()
    {
        var dep = new PluginDependency();
        dep.PluginId.Should().BeEmpty();
        dep.MinVersion.Should().BeNull();
        dep.MaxVersion.Should().BeNull();
        dep.IsOptional.Should().BeFalse();
        dep.Reason.Should().BeNull();
    }

    [Fact]
    public void PluginDependency_SetProperties_ShouldRetainValues()
    {
        var dep = new PluginDependency
        {
            PluginId = "dep-plugin",
            MinVersion = "1.0.0",
            MaxVersion = "2.0.0",
            IsOptional = true,
            Reason = "Feature X"
        };
        dep.PluginId.Should().Be("dep-plugin");
        dep.IsOptional.Should().BeTrue();
    }

    #endregion

    #region DependencyResolutionResult Defaults Tests

    [Fact]
    public void DependencyResolutionResult_Defaults_ShouldHaveExpectedValues()
    {
        var r = new DependencyResolutionResult();
        r.Success.Should().BeFalse();
        r.LoadOrder.Should().BeEmpty();
        r.ErrorMessage.Should().BeNull();
        r.MissingDependencies.Should().BeEmpty();
        r.CircularDependencies.Should().BeEmpty();
        r.VersionConflicts.Should().BeEmpty();
    }

    #endregion

    #region VersionConflict Defaults Tests

    [Fact]
    public void VersionConflict_Defaults_ShouldHaveExpectedValues()
    {
        var vc = new VersionConflict();
        vc.PluginId.Should().BeEmpty();
        vc.RequiredVersion.Should().BeEmpty();
        vc.ActualVersion.Should().BeEmpty();
        vc.RequiredBy.Should().BeEmpty();
    }

    #endregion

    #region DependencyGraph Model Tests

    [Fact]
    public void GraphNode_Defaults_ShouldHaveExpectedValues()
    {
        var n = new GraphNode();
        n.Id.Should().BeEmpty();
        n.Name.Should().BeEmpty();
        n.Version.Should().BeEmpty();
        n.IsInstalled.Should().BeFalse();
        n.Position.Should().BeNull();
    }

    [Fact]
    public void GraphEdge_Defaults_ShouldHaveExpectedValues()
    {
        var e = new GraphEdge();
        e.From.Should().BeEmpty();
        e.To.Should().BeEmpty();
        e.IsOptional.Should().BeFalse();
        e.VersionRequirement.Should().BeNull();
    }

    [Fact]
    public void DependencyGraph_Defaults_ShouldHaveEmptyLists()
    {
        var g = new DependencyGraph();
        g.Nodes.Should().BeEmpty();
        g.Edges.Should().BeEmpty();
    }

    #endregion

    #region ResolveDependencies No Dependencies Tests

    [Fact]
    public void ResolveDependencies_NoDependencies_ShouldSucceed()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new()
        };
        var result = _resolver.ResolveDependencies(plugins);
        result.Success.Should().BeTrue();
        result.LoadOrder.Should().HaveCount(2);
    }

    [Fact]
    public void ResolveDependencies_Empty_ShouldSucceed()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>();
        var result = _resolver.ResolveDependencies(plugins);
        result.Success.Should().BeTrue();
        result.LoadOrder.Should().BeEmpty();
    }

    [Fact]
    public void ResolveDependencies_SinglePlugin_ShouldSucceed()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new()
        };
        var result = _resolver.ResolveDependencies(plugins);
        result.Success.Should().BeTrue();
        result.LoadOrder.Should().ContainSingle("p1");
    }

    #endregion

    #region ResolveDependencies Linear Dependencies Tests

    [Fact]
    public void ResolveDependencies_LinearDependency_ShouldResolveCorrectly()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new() { new() { PluginId = "p1" } }
        };
        var result = _resolver.ResolveDependencies(plugins);
        result.Success.Should().BeTrue();
        result.LoadOrder.IndexOf("p1").Should().BeLessThan(result.LoadOrder.IndexOf("p2"));
    }

    [Fact]
    public void ResolveDependencies_DeepChain_ShouldResolveCorrectly()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new() { new() { PluginId = "p1" } },
            ["p3"] = new() { new() { PluginId = "p2" } }
        };
        var result = _resolver.ResolveDependencies(plugins);
        result.Success.Should().BeTrue();
        result.LoadOrder.IndexOf("p1").Should().BeLessThan(result.LoadOrder.IndexOf("p2"));
        result.LoadOrder.IndexOf("p2").Should().BeLessThan(result.LoadOrder.IndexOf("p3"));
    }

    #endregion

    #region ResolveDependencies Circular Detection Tests

    [Fact]
    public void ResolveDependencies_TwoNodeCycle_ShouldDetectCircular()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new() { new() { PluginId = "p2" } },
            ["p2"] = new() { new() { PluginId = "p1" } }
        };
        var result = _resolver.ResolveDependencies(plugins);
        result.Success.Should().BeFalse();
        result.CircularDependencies.Should().NotBeEmpty();
    }

    [Fact]
    public void ResolveDependencies_ThreeNodeCycle_ShouldDetectCircular()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new() { new() { PluginId = "p2" } },
            ["p2"] = new() { new() { PluginId = "p3" } },
            ["p3"] = new() { new() { PluginId = "p1" } }
        };
        var result = _resolver.ResolveDependencies(plugins);
        result.Success.Should().BeFalse();
        result.CircularDependencies.Should().NotBeEmpty();
    }

    [Fact]
    public void ResolveDependencies_SelfDependency_ShouldDetectCircular()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new() { new() { PluginId = "p1" } }
        };
        var result = _resolver.ResolveDependencies(plugins);
        result.Success.Should().BeFalse();
        result.CircularDependencies.Should().NotBeEmpty();
    }

    #endregion

    #region ResolveDependencies Optional Dependency Tests

    [Fact]
    public void ResolveDependencies_OptionalDependency_ShouldSucceed()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new() { new() { PluginId = "missing", IsOptional = true } }
        };
        var result = _resolver.ResolveDependencies(plugins);
        result.Success.Should().BeTrue();
    }

    #endregion

    #region ResolveDependencies Version Validation Tests

    [Fact]
    public void ResolveDependencies_VersionMismatch_ShouldDetectConflict()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new() { new() { PluginId = "p1", MinVersion = "2.0.0" } }
        };
        var versions = new Dictionary<string, string> { ["p1"] = "1.0.0" };
        var result = _resolver.ResolveDependencies(plugins, versions);
        result.Success.Should().BeFalse();
        result.VersionConflicts.Should().NotBeEmpty();
    }

    [Fact]
    public void ResolveDependencies_VersionMatch_ShouldSucceed()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new() { new() { PluginId = "p1", MinVersion = "1.0.0" } }
        };
        var versions = new Dictionary<string, string> { ["p1"] = "2.0.0" };
        var result = _resolver.ResolveDependencies(plugins, versions);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void ResolveDependencies_NoVersions_ShouldSkipValidation()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new() { new() { PluginId = "p1", MinVersion = "99.0.0" } }
        };
        var result = _resolver.ResolveDependencies(plugins, null);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void ResolveDependencies_MaxVersionExceeded_ShouldDetectConflict()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new() { new() { PluginId = "p1", MaxVersion = "1.0.0" } }
        };
        var versions = new Dictionary<string, string> { ["p1"] = "2.0.0" };
        var result = _resolver.ResolveDependencies(plugins, versions);
        result.Success.Should().BeFalse();
        result.VersionConflicts.Should().NotBeEmpty();
    }

    #endregion

    #region ValidateInstallation Tests

    [Fact]
    public void ValidateInstallation_AllDependenciesMet_ShouldReturnTrue()
    {
        var deps = new List<PluginDependency>
        {
            new() { PluginId = "dep1" },
            new() { PluginId = "dep2" }
        };
        var installed = new Dictionary<string, string>
        {
            ["dep1"] = "1.0.0",
            ["dep2"] = "1.0.0"
        };
        _resolver.ValidateInstallation("plugin", deps, installed).Should().BeTrue();
    }

    [Fact]
    public void ValidateInstallation_MissingDependency_ShouldReturnFalse()
    {
        var deps = new List<PluginDependency>
        {
            new() { PluginId = "dep1" },
            new() { PluginId = "missing" }
        };
        var installed = new Dictionary<string, string> { ["dep1"] = "1.0.0" };
        _resolver.ValidateInstallation("plugin", deps, installed).Should().BeFalse();
    }

    [Fact]
    public void ValidateInstallation_OptionalMissing_ShouldReturnTrue()
    {
        var deps = new List<PluginDependency>
        {
            new() { PluginId = "missing", IsOptional = true }
        };
        var installed = new Dictionary<string, string>();
        _resolver.ValidateInstallation("plugin", deps, installed).Should().BeTrue();
    }

    [Fact]
    public void ValidateInstallation_VersionMismatch_ShouldReturnFalse()
    {
        var deps = new List<PluginDependency>
        {
            new() { PluginId = "dep1", MinVersion = "2.0.0" }
        };
        var installed = new Dictionary<string, string> { ["dep1"] = "1.0.0" };
        _resolver.ValidateInstallation("plugin", deps, installed).Should().BeFalse();
    }

    [Fact]
    public void ValidateInstallation_NoDependencies_ShouldReturnTrue()
    {
        _resolver.ValidateInstallation("plugin", new(), new()).Should().BeTrue();
    }

    #endregion

    #region GetDependentPlugins Tests

    [Fact]
    public void GetDependentPlugins_NoDependents_ShouldReturnEmpty()
    {
        var allPlugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new()
        };
        _resolver.GetDependentPlugins("p1", allPlugins).Should().BeEmpty();
    }

    [Fact]
    public void GetDependentPlugins_HasDependents_ShouldReturnThem()
    {
        var allPlugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new() { new() { PluginId = "p1" } },
            ["p3"] = new() { new() { PluginId = "p1" } }
        };
        var dependents = _resolver.GetDependentPlugins("p1", allPlugins);
        dependents.Should().Contain(new[] { "p2", "p3" });
    }

    [Fact]
    public void GetDependentPlugins_OptionalDependents_ShouldNotReturnThem()
    {
        var allPlugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new() { new() { PluginId = "p1", IsOptional = true } }
        };
        _resolver.GetDependentPlugins("p1", allPlugins).Should().BeEmpty();
    }

    [Fact]
    public void GetDependentPlugins_NonExistent_ShouldReturnEmpty()
    {
        var allPlugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new()
        };
        _resolver.GetDependentPlugins("nonexistent", allPlugins).Should().BeEmpty();
    }

    #endregion

    #region DetectCircularDependencies Tests

    [Fact]
    public void DetectCircularDependencies_NoCycles_ShouldReturnEmpty()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new() { new() { PluginId = "p1" } }
        };
        _resolver.DetectCircularDependencies(plugins).Should().BeEmpty();
    }

    [Fact]
    public void DetectCircularDependencies_WithCycle_ShouldReturnNonEmpty()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new() { new() { PluginId = "p2" } },
            ["p2"] = new() { new() { PluginId = "p1" } }
        };
        _resolver.DetectCircularDependencies(plugins).Should().NotBeEmpty();
    }

    [Fact]
    public void DetectCircularDependencies_Empty_ShouldReturnEmpty()
    {
        _resolver.DetectCircularDependencies(new()).Should().BeEmpty();
    }

    #endregion

    #region GetDependencyGraph Tests

    [Fact]
    public void GetDependencyGraph_ShouldReturnGraph()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new(),
            ["p2"] = new() { new() { PluginId = "p1" } }
        };
        var graph = _resolver.GetDependencyGraph(plugins);
        graph.Should().NotBeNull();
        graph.Nodes.Should().HaveCount(2);
        graph.Edges.Should().HaveCount(1);
    }

    [Fact]
    public void GetDependencyGraph_Empty_ShouldReturnEmptyGraph()
    {
        var graph = _resolver.GetDependencyGraph(new());
        graph.Nodes.Should().BeEmpty();
        graph.Edges.Should().BeEmpty();
    }

    [Fact]
    public void GetDependencyGraph_WithVersions_ShouldIncludeVersionInfo()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new()
        };
        var versions = new Dictionary<string, string> { ["p1"] = "1.2.3" };
        var graph = _resolver.GetDependencyGraph(plugins, versions);
        graph.Nodes.Should().ContainSingle(n => n.Version == "1.2.3");
    }

    [Fact]
    public void GetDependencyGraph_OptionalEdge_ShouldMarkOptional()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>
        {
            ["p1"] = new() { new() { PluginId = "p2", IsOptional = true } }
        };
        var graph = _resolver.GetDependencyGraph(plugins);
        graph.Edges.Should().ContainSingle(e => e.IsOptional);
    }

    #endregion
}