using System.Linq;
using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

/// <summary>
/// Parity coverage for the Avalonia network-acceleration page helpers against the
/// WPF reference behavior (recommended target groups, star-menu ordering, target
/// search filtering and per-group selection counts).
/// </summary>
public sealed class NetworkAccelerationParityTests
{
    private static NetworkAccelerationGroupState Group(
        string id,
        bool enabled = false,
        bool isFavorite = false,
        int domainCount = 3,
        string displayName = "Group",
        string description = "Group description") =>
        new(id, displayName, description, enabled, isFavorite, domainCount);

    [Fact]
    public void RecommendedGroupIds_MatchesWpfReferenceSet()
    {
        var wpfReference = new[] { "steam", "github", "public-cdn", "twitch", "roblox" };

        NetworkAccelerationTargets.RecommendedGroupIds
            .OrderBy(id => id)
            .Should()
            .Equal(wpfReference.OrderBy(id => id));
    }

    [Fact]
    public void IsRecommendedGroup_DefaultIdsAreRecommendedWithoutFavorite()
    {
        foreach (var id in NetworkAccelerationTargets.RecommendedGroupIds)
            NetworkAccelerationTargets.IsRecommendedGroup(id, isFavorite: false).Should().BeTrue();
    }

    [Fact]
    public void IsRecommendedGroup_NonDefaultIdRequiresFavorite()
    {
        NetworkAccelerationTargets.IsRecommendedGroup("custom-cdn", isFavorite: false).Should().BeFalse();
        NetworkAccelerationTargets.IsRecommendedGroup("custom-cdn", isFavorite: true).Should().BeTrue();
    }

    [Fact]
    public void IsRecommendedGroup_IgnoresCase()
    {
        NetworkAccelerationTargets.IsRecommendedGroup("Steam", isFavorite: false).Should().BeTrue();
        NetworkAccelerationTargets.IsRecommendedGroup("PUBLIC-CDN", isFavorite: false).Should().BeTrue();
    }

    [Fact]
    public void GetRecommendedGroups_OrdersFavoritesFirstPreservingOriginalOrder()
    {
        var groups = new[]
        {
            Group("twitch"),
            Group("steam", isFavorite: true),
            Group("custom-cdn", isFavorite: true),
            Group("github"),
            Group("roblox"),
        };

        var recommended = NetworkAccelerationTargets.GetRecommendedGroups(groups);

        recommended.Select(group => group.Id)
            .Should()
            .Equal(new[] { "steam", "custom-cdn", "twitch", "github", "roblox" });
    }

    [Fact]
    public void GetRecommendedGroups_CapsAtEightEntries()
    {
        var groups = Enumerable.Range(0, 12)
            .Select(index => Group($"group-{index}", isFavorite: true, domainCount: 1))
            .ToArray();

        var recommended = NetworkAccelerationTargets.GetRecommendedGroups(groups);

        recommended.Should().HaveCount(8);
    }

    [Fact]
    public void GetRecommendedGroups_EmptyInputReturnsEmpty()
    {
        NetworkAccelerationTargets.GetRecommendedGroups([]).Should().BeEmpty();
    }

    [Fact]
    public void FilterGroups_EmptyQueryReturnsAllGroups()
    {
        var groups = new[] { Group("steam"), Group("github") };

        var filtered = NetworkAccelerationTargets.FilterGroups(groups, null);

        filtered.Should().Equal(groups);
    }

    [Fact]
    public void FilterGroups_MatchesDisplayNameCaseInsensitive()
    {
        var groups = new[] { Group("steam", displayName: "Steam"), Group("github", displayName: "GitHub") };

        var filtered = NetworkAccelerationTargets.FilterGroups(groups, "stEa");

        filtered.Should().ContainSingle().Which.Id.Should().Be("steam");
    }

    [Fact]
    public void FilterGroups_MatchesDescription()
    {
        var groups = new[] { Group("twitch", description: "Twitch live streaming"), Group("roblox") };

        var filtered = NetworkAccelerationTargets.FilterGroups(groups, "streaming");

        filtered.Should().ContainSingle().Which.Id.Should().Be("twitch");
    }

    [Fact]
    public void FilterGroups_NoMatchReturnsEmpty()
    {
        var groups = new[] { Group("steam", displayName: "Steam") };

        NetworkAccelerationTargets.FilterGroups(groups, "nothing-here").Should().BeEmpty();
    }

    [Fact]
    public void GetSelectedDomainCount_EnabledGroupCountsAllDomains()
    {
        NetworkAccelerationTargets.GetSelectedDomainCount(Group("steam", enabled: true, domainCount: 7))
            .Should()
            .Be(7);
    }

    [Fact]
    public void GetSelectedDomainCount_DisabledGroupCountsNothing()
    {
        NetworkAccelerationTargets.GetSelectedDomainCount(Group("steam", enabled: false, domainCount: 7))
            .Should()
            .Be(0);
    }
}
