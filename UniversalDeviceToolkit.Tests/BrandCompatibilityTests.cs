using FluentAssertions;
using LenovoLegionToolkit.Lib.Branding;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class BrandCompatibilityTests
{
    [Fact]
    public void ProductDisplayName_IsNonEmpty()
    {
        BrandCompatibility.ProductDisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ProductCompactName_IsNonEmpty()
    {
        BrandCompatibility.ProductCompactName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void LegacyProductDisplayName_IsNonEmpty()
    {
        BrandCompatibility.LegacyProductDisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void LegacyAssemblyLib_IsLenovoLegionToolkitLib()
    {
        BrandCompatibility.LegacyAssemblyLib.Should().Be("LenovoLegionToolkit.Lib");
        BrandCompatibility.LegacyAssemblyLib.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void LegacyAssemblyLibPlugins_IsNonEmpty()
    {
        BrandCompatibility.LegacyAssemblyLibPlugins.Should().Be("LenovoLegionToolkit.Lib.Plugins");
        BrandCompatibility.LegacyAssemblyLibPlugins.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PreferredAssemblyNames_AreNonEmptyPlanningTokens()
    {
        BrandCompatibility.PreferredAssemblyLib.Should().NotBeNullOrWhiteSpace();
        BrandCompatibility.PreferredAssemblyLibPlugins.Should().NotBeNullOrWhiteSpace();
    }
}
