using LibreHardwareMonitor.Hardware;
using Yomi.App.Services.Providers;
using Yomi.Tests.Fakes;

namespace Yomi.Tests;

public class CpuMetricsProviderTests
{
    [Fact]
    public void GetMetrics_UsesMaxCoreClockAndTotalLoad()
    {
        var cpu = new FakeHardware("AMD Ryzen 9 7950X", HardwareType.Cpu,
            new FakeSensor("CPU Total", SensorType.Load, 7f),
            new FakeSensor("CPU Core #1", SensorType.Clock, 4140f),
            new FakeSensor("CPU Core #2", SensorType.Clock, 3800f));

        var provider = new CpuMetricsProvider();
        var result = provider.GetMetrics([cpu]);

        Assert.Equal(7f, result.UsagePercent);
        Assert.Equal(4.14, result.ClockGHz!.Value, precision: 2);
        Assert.Equal("AMD Ryzen 9 7950X", result.Name);
    }

    [Fact]
    public void GetMetrics_NoCpuHardware_ReturnsAllNull()
    {
        var gpu = new FakeHardware("Some GPU", HardwareType.GpuNvidia);

        var provider = new CpuMetricsProvider();
        var result = provider.GetMetrics([gpu]);

        Assert.Null(result.UsagePercent);
        Assert.Null(result.ClockGHz);
        Assert.Null(result.Name);
    }
}
