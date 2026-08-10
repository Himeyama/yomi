using LibreHardwareMonitor.Hardware;
using Yomi.App.Services.Providers;
using Yomi.Tests.Fakes;

namespace Yomi.Tests;

public class MemoryMetricsProviderTests
{
    [Fact]
    public void GetMetrics_ComputesTotalFromUsedAndAvailable()
    {
        var memory = new FakeHardware("Memory", HardwareType.Memory,
            new FakeSensor("Memory", SensorType.Load, 34f),
            new FakeSensor("Memory Used", SensorType.Data, 43f),
            new FakeSensor("Memory Available", SensorType.Data, 85f));

        var provider = new MemoryMetricsProvider();
        var result = provider.GetMetrics([memory]);

        Assert.Equal(34f, result.UsagePercent);
        Assert.Equal(43f, result.UsedGiB);
        Assert.Equal(128.0, result.TotalGiB!.Value, precision: 3);
    }

    [Fact]
    public void GetMetrics_NoMemoryHardware_ReturnsAllNull()
    {
        var cpu = new FakeHardware("Some CPU", HardwareType.Cpu);

        var provider = new MemoryMetricsProvider();
        var result = provider.GetMetrics([cpu]);

        Assert.Null(result.UsagePercent);
        Assert.Null(result.UsedGiB);
        Assert.Null(result.TotalGiB);
    }
}
