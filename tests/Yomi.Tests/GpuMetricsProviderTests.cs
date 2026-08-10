using LibreHardwareMonitor.Hardware;
using Yomi.App.Services.Providers;
using Yomi.Tests.Fakes;

namespace Yomi.Tests;

public class GpuMetricsProviderTests
{
    [Fact]
    public void GetMetrics_NvidiaSensorNames_ReturnsUsageAndVram()
    {
        var gpu = new FakeHardware("GeForce RTX 4090", HardwareType.GpuNvidia,
            new FakeSensor("GPU Core", SensorType.Load, 42f),
            new FakeSensor("GPU Memory Used", SensorType.SmallData, 2048f),
            new FakeSensor("GPU Memory Total", SensorType.SmallData, 8192f));

        var provider = new GpuMetricsProvider();
        var result = provider.GetMetrics([gpu]);

        Assert.Equal(42f, result.UsagePercent);
        Assert.Equal(25.0, result.VramUsagePercent);
        Assert.Equal(2.0, result.VramUsedGiB!.Value, precision: 3);
        Assert.Equal(8.0, result.VramTotalGiB!.Value, precision: 3);
        Assert.Equal("GeForce RTX 4090", result.Name);
    }

    [Fact]
    public void GetMetrics_AmdD3DSensorNames_FallsBackToPartialMatch()
    {
        var gpu = new FakeHardware("AMD Radeon RX 7900", HardwareType.GpuAmd,
            new FakeSensor("D3D 3D", SensorType.Load, 10f),
            new FakeSensor("D3D Dedicated Memory Used", SensorType.SmallData, 1024f),
            new FakeSensor("D3D Dedicated Memory Total", SensorType.SmallData, 16384f));

        var provider = new GpuMetricsProvider();
        var result = provider.GetMetrics([gpu]);

        Assert.Equal(10f, result.UsagePercent);
        Assert.Equal(6.25, result.VramUsagePercent!.Value, precision: 3);
    }

    [Fact]
    public void GetMetrics_NoGpuHardware_ReturnsAllNull()
    {
        var cpu = new FakeHardware("Some CPU", HardwareType.Cpu);

        var provider = new GpuMetricsProvider();
        var result = provider.GetMetrics([cpu]);

        Assert.Null(result.UsagePercent);
        Assert.Null(result.VramUsagePercent);
        Assert.Null(result.Name);
    }

    [Fact]
    public void GetMetrics_TotalVramZero_DoesNotDivideByZero()
    {
        var gpu = new FakeHardware("Broken GPU", HardwareType.GpuIntel,
            new FakeSensor("GPU Memory Used", SensorType.SmallData, 0f),
            new FakeSensor("GPU Memory Total", SensorType.SmallData, 0f));

        var provider = new GpuMetricsProvider();
        var result = provider.GetMetrics([gpu]);

        Assert.Null(result.VramUsagePercent);
    }
}
