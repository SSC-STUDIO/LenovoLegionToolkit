using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class DependencyResolverTests
{
    private readonly DependencyResolver _resolver = new();

    private static void AssertBeforeInOrder(IReadOnlyList<string> order, string beforeId, string afterId)
    {
        var list = order as IList<string> ?? order.ToList();
        var iBefore = list.IndexOf(beforeId);
        var iAfter = list.IndexOf(afterId);
        iBefore.Should().BeGreaterThanOrEqualTo(0);
        iAfter.Should().BeGreaterThanOrEqualTo(0);
        iBefore.Should().BeLessThan(iAfter);
    }

    [Fact]
    public void ResolveDependencies_WhenLinearChain_ReturnsTopologicalOrder()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["C"] = [new PluginDependency { PluginId = "B" }],
            ["B"] = [new PluginDependency { PluginId = "A" }],
            ["A"] = []
        };

        var result = _resolver.ResolveDependencies(plugins);

        result.Success.Should().BeTrue();
        AssertBeforeInOrder(result.LoadOrder, "A", "B");
        AssertBeforeInOrder(result.LoadOrder, "B", "C");
    }

    [Fact]
    public void ResolveDependencies_WhenDiamond_ReturnsValidTopologicalOrder()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["D"] =
            [
                new PluginDependency { PluginId = "B" },
                new PluginDependency { PluginId = "C" }
            ],
            ["B"] = [new PluginDependency { PluginId = "A" }],
            ["C"] = [new PluginDependency { PluginId = "A" }],
            ["A"] = []
        };

        var result = _resolver.ResolveDependencies(plugins);

        result.Success.Should().BeTrue();
        AssertBeforeInOrder(result.LoadOrder, "A", "D");
        var iB = result.LoadOrder.IndexOf("B");
        var iC = result.LoadOrder.IndexOf("C");
        iB.Should().BeGreaterThan(result.LoadOrder.IndexOf("A"));
        iC.Should().BeGreaterThan(result.LoadOrder.IndexOf("A"));
    }

    [Fact]
    public void ResolveDependencies_WhenOptionalDependency_DoesNotConstrainTopology()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] =
            [
                new PluginDependency { PluginId = "opt", IsOptional = true }
            ],
            ["opt"] = []
        };

        var result = _resolver.ResolveDependencies(plugins);

        result.Success.Should().BeTrue();
        result.LoadOrder.Should().HaveCount(2);
    }

    [Fact]
    public void DetectCircularDependencies_WhenCycle_ReturnsNonEmpty()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = [new PluginDependency { PluginId = "B" }],
            ["B"] = [new PluginDependency { PluginId = "A" }]
        };

        var cycles = _resolver.DetectCircularDependencies(plugins);

        cycles.Should().NotBeEmpty();
    }

    [Fact]
    public void ResolveDependencies_WhenCycle_ReturnsFailure()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = [new PluginDependency { PluginId = "B" }],
            ["B"] = [new PluginDependency { PluginId = "A" }]
        };

        var result = _resolver.ResolveDependencies(plugins);

        result.Success.Should().BeFalse();
        result.CircularDependencies.Should().NotBeEmpty();
    }

    [Fact]
    public void ResolveDependencies_WhenEmptyVersionDictionary_SkipsVersionValidation()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = [new PluginDependency { PluginId = "dep", MinVersion = "99.0.0" }],
            ["dep"] = []
        };

        var result = _resolver.ResolveDependencies(plugins, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        result.Success.Should().BeTrue();
        result.VersionConflicts.Should().BeEmpty();
    }

    [Fact]
    public void ResolveDependencies_WhenMaxVersionViolated_ReportsVersionConflict()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] =
            [
                new PluginDependency { PluginId = "dep", MaxVersion = "1.0.0" }
            ],
            ["dep"] = []
        };
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["dep"] = "2.0.0" };

        var result = _resolver.ResolveDependencies(plugins, versions);

        result.Success.Should().BeFalse();
        result.VersionConflicts.Should().ContainSingle(c =>
            c.PluginId.Equals("dep", StringComparison.OrdinalIgnoreCase)
            && c.RequiredBy.Equals("host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateInstallation_WhenRequiredDependencyMissing_ReturnsFalse()
    {
        var deps = new List<PluginDependency>
        {
            new() { PluginId = "need", IsOptional = false }
        };
        var installed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        _resolver.ValidateInstallation("plugin", deps, installed).Should().BeFalse();
    }

    [Fact]
    public void ValidateInstallation_WhenOptionalDependencyMissing_ReturnsTrue()
    {
        var deps = new List<PluginDependency>
        {
            new() { PluginId = "opt", IsOptional = true }
        };
        var installed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        _resolver.ValidateInstallation("plugin", deps, installed).Should().BeTrue();
    }

    [Fact]
    public void ValidateInstallation_WhenVersionOutOfRange_ReturnsFalse()
    {
        var deps = new List<PluginDependency>
        {
            new() { PluginId = "dep", MinVersion = "2.0.0", IsOptional = false }
        };
        var installed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["dep"] = "1.0.0" };

        _resolver.ValidateInstallation("plugin", deps, installed).Should().BeFalse();
    }

    [Fact]
    public void ValidateInstallation_WhenUnparsableInstalledVersion_TreatedAsCompatible()
    {
        var deps = new List<PluginDependency>
        {
            new() { PluginId = "dep", MinVersion = "2.0.0", IsOptional = false }
        };
        var installed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["dep"] = "not-a-version" };

        _resolver.ValidateInstallation("plugin", deps, installed).Should().BeTrue();
    }

    [Fact]
    public void GetDependentPlugins_IgnoresOptionalAndUsesCaseInsensitivePluginId()
    {
        var all = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["RequiredChild"] = [new PluginDependency { PluginId = "BASE", IsOptional = false }],
            ["OptionalOnly"] = [new PluginDependency { PluginId = "base", IsOptional = true }],
            ["AlsoRequired"] = [new PluginDependency { PluginId = "BaSe", IsOptional = false }]
        };

        var dependents = _resolver.GetDependentPlugins("base", all);

        dependents.Should().BeEquivalentTo("RequiredChild", "AlsoRequired");
    }

    [Fact]
    public void GetDependencyGraph_AddsExternalDependencyNode()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = [new PluginDependency { PluginId = "external-only" }]
        };

        var graph = _resolver.GetDependencyGraph(plugins);

        graph.Nodes.Select(n => n.Id).Should().Contain("external-only");
        graph.Edges.Should().ContainSingle(e =>
            e.From.Equals("host", StringComparison.OrdinalIgnoreCase)
            && e.To.Equals("external-only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolveDependencies_WhenNoVersionMetadata_SucceedsEvenIfDependencyDeclaresMinVersion()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] =
            [
                new PluginDependency { PluginId = "dep", MinVersion = "2.0.0" }
            ],
            ["dep"] = []
        };

        var result = _resolver.ResolveDependencies(plugins, pluginVersionsById: null);

        result.Success.Should().BeTrue();
        result.VersionConflicts.Should().BeEmpty();
        result.LoadOrder.Should().Equal("dep", "host");
    }

    [Fact]
    public void ResolveDependencies_WhenVersionMetadataShowsConflict_SetsFailureAndVersionConflicts()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] =
            [
                new PluginDependency { PluginId = "dep", MinVersion = "2.0.0" }
            ],
            ["dep"] = []
        };
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["dep"] = "1.0.0" };

        var result = _resolver.ResolveDependencies(plugins, versions);

        result.Success.Should().BeFalse();
        var c = result.VersionConflicts.Single();
        c.PluginId.Should().Be("dep");
        c.ActualVersion.Should().Be("1.0.0");
        c.RequiredBy.Should().Be("host");
    }

    [Fact]
    public void ResolveDependencies_WhenVersionMetadataSatisfiesRange_Succeeds()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] =
            [
                new PluginDependency { PluginId = "dep", MinVersion = "2.0.0", MaxVersion = "3.0.0" }
            ],
            ["dep"] = []
        };
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["dep"] = "2.5.0" };

        var result = _resolver.ResolveDependencies(plugins, versions);

        result.Success.Should().BeTrue();
        result.VersionConflicts.Should().BeEmpty();
    }

    [Fact]
    public void GetDependencyGraph_WhenNoVersions_UsesQuestionMarkForKnownNodes()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = [new PluginDependency { PluginId = "dep" }],
            ["dep"] = []
        };

        var graph = _resolver.GetDependencyGraph(plugins, pluginVersionsById: null);

        graph.Nodes.Should().Contain(n => n.Id.Equals("host", StringComparison.OrdinalIgnoreCase) && n.Version == "?");
        graph.Nodes.Should().Contain(n => n.Id.Equals("dep", StringComparison.OrdinalIgnoreCase) && n.Version == "?");
    }

    [Fact]
    public void GetDependencyGraph_WhenVersionsProvided_MapsVersionsToNodes()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = [new PluginDependency { PluginId = "dep" }],
            ["dep"] = []
        };
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = "4.0.0",
            ["dep"] = "1.2.3"
        };

        var graph = _resolver.GetDependencyGraph(plugins, versions);

        graph.Nodes.First(n => n.Id.Equals("dep", StringComparison.OrdinalIgnoreCase)).Version.Should().Be("1.2.3");
        graph.Nodes.First(n => n.Id.Equals("host", StringComparison.OrdinalIgnoreCase)).Version.Should().Be("4.0.0");
    }

    [Fact]
    public void ResolveDependencies_WhenActualVersionUnparsable_SkipsConflictAndSucceeds()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] =
            [
                new PluginDependency { PluginId = "dep", MinVersion = "2.0.0" }
            ],
            ["dep"] = []
        };
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["dep"] = "not-semver" };

        var result = _resolver.ResolveDependencies(plugins, versions);

        result.Success.Should().BeTrue();
        result.VersionConflicts.Should().BeEmpty();
    }
}
