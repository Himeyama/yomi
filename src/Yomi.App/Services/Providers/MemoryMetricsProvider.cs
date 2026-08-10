using LibreHardwareMonitor.Hardware;

namespace Yomi.App.Services.Providers;

public sealed class MemoryMetricsProvider : IMemoryMetricsProvider
{
    public MemoryMetrics GetMetrics(IReadOnlyList<IHardware> hardware)
    {
        var memory = hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
        if (memory is null) return new MemoryMetrics(null, null, null);

        var load = memory.Sensors.FirstOrDefault(s =>
            s.SensorType == SensorType.Load && s.Name == "Memory");

        // LibreHardwareMonitorLibのMemoryセンサーはGB(GiB相当)単位で値を返す。
        var used = memory.Sensors.FirstOrDefault(s =>
            s.SensorType == SensorType.Data && s.Name == "Memory Used");
        var available = memory.Sensors.FirstOrDefault(s =>
            s.SensorType == SensorType.Data && s.Name == "Memory Available");

        double? totalGiB = used?.Value.HasValue == true && available?.Value.HasValue == true
            ? used.Value.Value + available.Value.Value
            : null;

        return new MemoryMetrics(load?.Value, used?.Value, totalGiB);
    }
}
