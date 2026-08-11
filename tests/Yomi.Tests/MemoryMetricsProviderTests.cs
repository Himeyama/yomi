using Yomi.App.Services.Providers;

namespace Yomi.Tests;

public class MemoryMetricsProviderTests
{
    private const ulong GiB = 1024UL * 1024 * 1024;

    [Fact]
    public void Compute_UsedIsTotalMinusAvailable()
    {
        // 総 16GiB、空き 6GiB → 使用 10GiB、使用率 62.5%
        var result = MemoryMetricsProvider.Compute(16 * GiB, 6 * GiB);

        Assert.Equal(62.5, result.UsagePercent!.Value, precision: 3);
        Assert.Equal(10.0, result.UsedGiB!.Value, precision: 3);
        Assert.Equal(16.0, result.TotalGiB!.Value, precision: 3);
    }

    [Fact]
    public void Compute_ZeroTotal_ReturnsAllNull()
    {
        var result = MemoryMetricsProvider.Compute(0, 0);

        Assert.Null(result.UsagePercent);
        Assert.Null(result.UsedGiB);
        Assert.Null(result.TotalGiB);
    }

    [Fact]
    public void Compute_FullyAvailable_ZeroUsage()
    {
        var result = MemoryMetricsProvider.Compute(8 * GiB, 8 * GiB);

        Assert.Equal(0.0, result.UsagePercent!.Value, precision: 3);
        Assert.Equal(0.0, result.UsedGiB!.Value, precision: 3);
    }
}
