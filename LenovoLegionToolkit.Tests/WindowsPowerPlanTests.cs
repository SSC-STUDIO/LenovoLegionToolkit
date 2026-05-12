using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace LenovoLegionToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class WindowsPowerPlanTests
{
    private static readonly Guid GuidA = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid GuidB = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    #region Equality

    [Fact]
    public void Equals_SameGuidDifferentNameAndActive_ShouldBeEqual()
    {
        var a = new WindowsPowerPlan(GuidA, "Balanced", true);
        var b = new WindowsPowerPlan(GuidA, "Performance", false);
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentGuid_ShouldNotBeEqual()
    {
        var a = new WindowsPowerPlan(GuidA, "Balanced", true);
        var b = new WindowsPowerPlan(GuidB, "Balanced", true);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Null_ShouldBeFalse()
    {
        var a = new WindowsPowerPlan(GuidA, "Balanced", true);
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_BoxedSameGuid_ShouldBeEqual()
    {
        var a = new WindowsPowerPlan(GuidA, "Balanced", true);
        object b = new WindowsPowerPlan(GuidA, "Other", false);
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void OperatorEquals_SameGuid_ShouldBeTrue()
    {
        var a = new WindowsPowerPlan(GuidA, "Balanced", true);
        var b = new WindowsPowerPlan(GuidA, "Other", false);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void OperatorNotEquals_DifferentGuid_ShouldBeTrue()
    {
        var a = new WindowsPowerPlan(GuidA, "Balanced", true);
        var b = new WindowsPowerPlan(GuidB, "Balanced", true);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameGuid_ShouldBeSame()
    {
        var a = new WindowsPowerPlan(GuidA, "Balanced", true);
        var b = new WindowsPowerPlan(GuidA, "Other", false);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentGuid_ShouldDiffer()
    {
        var a = new WindowsPowerPlan(GuidA, "Balanced", true);
        var b = new WindowsPowerPlan(GuidB, "Balanced", true);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    #endregion

    #region Properties

    [Fact]
    public void Properties_ShouldReturnConstructorValues()
    {
        var plan = new WindowsPowerPlan(GuidA, "Balanced", true);
        plan.Guid.Should().Be(GuidA);
        plan.Name.Should().Be("Balanced");
        plan.IsActive.Should().BeTrue();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldContainAllFields()
    {
        var plan = new WindowsPowerPlan(GuidA, "Balanced", true);
        var s = plan.ToString();
        s.Should().Contain(GuidA.ToString());
        s.Should().Contain("Balanced");
        s.Should().Contain("True");
    }

    #endregion
}
