using Yomi.App.Services.Providers;

namespace Yomi.Tests;

public class CpuMetricsProviderTests
{
    [Fact]
    public void ComputeClockGHz_BoostAbove100Percent_ScalesBaseClock()
    {
        // ベース 3.8GHz、性能比 107% → 実効 4.066GHz
        var result = CpuMetricsProvider.ComputeClockGHz(3800, 107);

        Assert.Equal(4.066, result!.Value, precision: 3);
    }

    [Fact]
    public void ComputeClockGHz_AtBaseClock_ReturnsBaseGHz()
    {
        var result = CpuMetricsProvider.ComputeClockGHz(3200, 100);

        Assert.Equal(3.2, result!.Value, precision: 3);
    }

    [Fact]
    public void ComputeClockGHz_NoBaseClock_ReturnsNull()
    {
        Assert.Null(CpuMetricsProvider.ComputeClockGHz(null, 100));
    }

    [Fact]
    public void ComputeClockGHz_ZeroPerformanceRatio_ReturnsNull()
    {
        Assert.Null(CpuMetricsProvider.ComputeClockGHz(3800, 0));
    }
}
