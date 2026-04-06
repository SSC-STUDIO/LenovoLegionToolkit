using System;
using Xunit;

namespace LenovoLegionToolkit.Plugins.Shared.Tests;

public class ConstantsTests
{
    [Fact]
    public void TimeoutValues_ArePositive()
    {
        Assert.True(Constants.DefaultTimeoutSeconds > 0);
        Assert.True(Constants.DownloadTimeoutSeconds > 0);
        Assert.True(Constants.ProcessTimeoutSeconds > 0);
    }

    [Fact]
    public void BufferSizes_ArePowerOfTwo()
    {
        Assert.True(IsPowerOfTwo(Constants.DefaultBufferSize));
        Assert.True(IsPowerOfTwo(Constants.LargeBufferSize));
    }

    [Fact]
    public void FileSizeLimits_AreReasonable()
    {
        Assert.True(Constants.MaxConfigFileSizeBytes > 0);
        Assert.True(Constants.MaxDownloadFileSizeBytes > Constants.MaxConfigFileSizeBytes);
    }

    [Fact]
    public void EstimatedDownloadSize_IsPositive()
    {
        Assert.True(Constants.EstimatedViveToolDownloadBytes > 0);
    }

    [Fact]
    public void MinLLTVersion_IsValidVersionString()
    {
        Assert.NotNull(Constants.MinLLTVersion);
        Assert.Matches(@"^\d+\.\d+\.\d+$", Constants.MinLLTVersion);
    }

    [Fact]
    public void RetryConfiguration_IsValid()
    {
        Assert.True(Constants.MaxRetryAttempts > 0);
        Assert.True(Constants.RetryDelayMs > 0);
    }

    private static bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }
}
