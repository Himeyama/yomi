using Yomi.App.Services.Providers;

namespace Yomi.Tests;

public class GpuMetricsProviderTests
{
    [Fact]
    public void ComputeVramUsagePercent_HalfUsed_Returns50()
    {
        var result = GpuMetricsProvider.ComputeVramUsagePercent(5.0, 10.0);

        Assert.Equal(50.0, result!.Value, precision: 3);
    }

    [Fact]
    public void ComputeVramUsagePercent_ZeroTotal_ReturnsNull()
    {
        Assert.Null(GpuMetricsProvider.ComputeVramUsagePercent(2.0, 0.0));
    }

    [Fact]
    public void ComputeVramUsagePercent_NoUsed_ReturnsNull()
    {
        Assert.Null(GpuMetricsProvider.ComputeVramUsagePercent(null, 10.0));
    }

    [Fact]
    public void ComputeVramUsagePercent_NoTotal_ReturnsNull()
    {
        Assert.Null(GpuMetricsProvider.ComputeVramUsagePercent(2.0, null));
    }
}
