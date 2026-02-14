using System;
using System.Reflection;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LenovoLegionToolkit.Tests.Utils;

[TestClass]
[TestCategory(TestCategories.Utils)]
public class ReflectionCacheTests : UnitTestBase
{
    private class TestClass
    {
        public int PublicProperty { get; set; } = 42;
        public string StringProperty { get; set; } = "test";
        private int PrivateProperty { get; set; }
    }

    [TestMethod]
    public void GetCachedProperties_ShouldReturnPublicProperties()
    {
        var properties = ReflectionCache.GetCachedProperties(typeof(TestClass));

        properties.Should().NotBeEmpty();
        properties.Should().Contain(p => p.Name == nameof(TestClass.PublicProperty));
        properties.Should().Contain(p => p.Name == nameof(TestClass.StringProperty));
    }

    [TestMethod]
    public void GetCachedProperties_WhenCalledMultipleTimes_ShouldReturnSameInstance()
    {
        var properties1 = ReflectionCache.GetCachedProperties(typeof(TestClass));
        var properties2 = ReflectionCache.GetCachedProperties(typeof(TestClass));

        properties1.Should().BeSameAs(properties2);
    }

    [TestMethod]
    public void GetCachedProperty_ShouldReturnCorrectProperty()
    {
        var property = ReflectionCache.GetCachedProperty(typeof(TestClass), nameof(TestClass.PublicProperty));

        property.Should().NotBeNull();
        property!.Name.Should().Be(nameof(TestClass.PublicProperty));
    }

    [TestMethod]
    public void GetCachedProperty_WhenPropertyDoesNotExist_ShouldReturnNull()
    {
        var property = ReflectionCache.GetCachedProperty(typeof(TestClass), "NonExistentProperty");

        property.Should().BeNull();
    }

    [TestMethod]
    public void GetCachedProperty_WhenCalledMultipleTimes_ShouldReturnSameInstance()
    {
        var property1 = ReflectionCache.GetCachedProperty(typeof(TestClass), nameof(TestClass.PublicProperty));
        var property2 = ReflectionCache.GetCachedProperty(typeof(TestClass), nameof(TestClass.PublicProperty));

        property1.Should().BeSameAs(property2);
    }

    [TestMethod]
    public void GetCachedPropertyValue_ShouldReturnCorrectValue()
    {
        var obj = new TestClass { PublicProperty = 100 };

        var value = ReflectionCache.GetCachedPropertyValue(obj, nameof(TestClass.PublicProperty));

        value.Should().Be(100);
    }

    [TestMethod]
    public void GetCachedPropertyValue_WhenObjectIsNull_ShouldReturnNull()
    {
        var value = ReflectionCache.GetCachedPropertyValue(null!, "AnyProperty");

        value.Should().BeNull();
    }

    [TestMethod]
    public void ClearCache_ShouldClearAllCaches()
    {
        var properties1 = ReflectionCache.GetCachedProperties(typeof(TestClass));
        var property1 = ReflectionCache.GetCachedProperty(typeof(TestClass), nameof(TestClass.PublicProperty));

        ReflectionCache.ClearCache();

        var properties2 = ReflectionCache.GetCachedProperties(typeof(TestClass));
        var property2 = ReflectionCache.GetCachedProperty(typeof(TestClass), nameof(TestClass.PublicProperty));

        properties2.Should().NotBeSameAs(properties1);
        property2.Should().NotBeSameAs(property1);
    }

    [TestCleanup]
    public new void Cleanup()
    {
        ReflectionCache.ClearCache();
    }
}

[TestClass]
[TestCategory(TestCategories.Utils)]
public class GPUPowerInfoCacheTests : UnitTestBase
{
    [TestMethod]
    public void Constructor_DefaultValues_ShouldInitializeCorrectly()
    {
        var cache = new GPUPowerInfoCache();

        var (wattage, voltage) = cache.GetCached();

        wattage.Should().Be(-1);
        voltage.Should().Be(0);
    }

    [TestMethod]
    public void Update_ShouldSetCachedValues()
    {
        var cache = new GPUPowerInfoCache();

        cache.Update(100, 1.2);
        var (wattage, voltage) = cache.GetCached();

        wattage.Should().Be(100);
        voltage.Should().Be(1.2);
    }

    [TestMethod]
    public void IsCacheValid_WhenNoUpdate_ShouldReturnFalse()
    {
        var cache = new GPUPowerInfoCache();

        cache.IsCacheValid().Should().BeFalse();
    }

    [TestMethod]
    public void IsCacheValid_AfterUpdate_ShouldReturnTrue()
    {
        var cache = new GPUPowerInfoCache();
        cache.Update(100, 1.2);

        cache.IsCacheValid().Should().BeTrue();
    }

    [TestMethod]
    public void IsCacheValid_AfterExpiration_ShouldReturnFalse()
    {
        var cache = new GPUPowerInfoCache(TimeSpan.FromMilliseconds(10));
        cache.Update(100, 1.2);

        System.Threading.Thread.Sleep(20);

        cache.IsCacheValid().Should().BeFalse();
    }

    [TestMethod]
    public void ShouldTryNvidiaSmi_Initially_ShouldReturnTrue()
    {
        var cache = new GPUPowerInfoCache();

        cache.ShouldTryNvidiaSmi().Should().BeTrue();
    }

    [TestMethod]
    public void ShouldTryNvidiaSmi_AfterFailure_ShouldReturnFalse()
    {
        var cache = new GPUPowerInfoCache();

        cache.MarkNvidiaSmiFailed();

        cache.ShouldTryNvidiaSmi().Should().BeFalse();
    }

    [TestMethod]
    public void ShouldTryNvidiaSmi_AfterRetryInterval_ShouldReturnTrue()
    {
        var cache = new GPUPowerInfoCache(nvidiaSmiRetryInterval: TimeSpan.FromMilliseconds(10));

        cache.MarkNvidiaSmiFailed();
        System.Threading.Thread.Sleep(20);

        cache.ShouldTryNvidiaSmi().Should().BeTrue();
    }

    [TestMethod]
    public void ResetNvidiaSmiFailed_ShouldAllowRetry()
    {
        var cache = new GPUPowerInfoCache();

        cache.MarkNvidiaSmiFailed();
        cache.ResetNvidiaSmiFailed();

        cache.ShouldTryNvidiaSmi().Should().BeTrue();
    }
}
