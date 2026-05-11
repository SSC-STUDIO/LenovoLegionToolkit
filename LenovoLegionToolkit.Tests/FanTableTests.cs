using System;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace LenovoLegionToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class FanTableTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValid10ElementArray_ShouldSucceed()
    {
        ushort[] data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var table = new FanTable(data);
        table.FSS0.Should().Be(1);
        table.FSS9.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithInvalidLength_ShouldThrow()
    {
        ushort[] data = [1, 2, 3];
        var act = () => new FanTable(data);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldSetFSTMToOne()
    {
        ushort[] data = new ushort[10];
        var table = new FanTable(data);
        table.FSTM.Should().Be(1);
    }

    #endregion

    #region GetTable Tests

    [Fact]
    public void GetTable_ShouldReturn10ElementArray()
    {
        ushort[] data = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
        var table = new FanTable(data);
        var result = table.GetTable();
        result.Should().HaveCount(10);
        result.Should().ContainInOrder(10, 20, 30, 40, 50, 60, 70, 80, 90, 100);
    }

    [Fact]
    public void GetTable_AfterRoundtrip_ShouldPreserveValues()
    {
        ushort[] data = [255, 128, 64, 32, 16, 8, 4, 2, 1, 0];
        var table = new FanTable(data);
        var result = table.GetTable();
        result.Should().ContainInOrder(data);
    }

    #endregion

    #region GetBytes Tests

    [Fact]
    public void GetBytes_ShouldReturn64ByteArray()
    {
        ushort[] data = new ushort[10];
        var table = new FanTable(data);
        var bytes = table.GetBytes();
        bytes.Length.Should().Be(64);
    }

    [Fact]
    public void GetBytes_FirstByteShouldBeFSTM()
    {
        ushort[] data = new ushort[10];
        var table = new FanTable(data);
        var bytes = table.GetBytes();
        bytes[0].Should().Be(1); // FSTM defaults to 1
    }

    #endregion
}
