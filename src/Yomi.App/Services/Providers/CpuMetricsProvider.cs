using LibreHardwareMonitor.Hardware;

namespace Yomi.App.Services.Providers;

public sealed class CpuMetricsProvider : ICpuMetricsProvider
{
    public CpuMetrics GetMetrics(IReadOnlyList<IHardware> hardware)
    {
        var cpu = hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu is null) return new CpuMetrics(null, null, null);

        var totalLoad = cpu.Sensors.FirstOrDefault(s =>
            s.SensorType == SensorType.Load && s.Name == "CPU Total");

        // コアごとのクロックセンサーの最大値を代表値として採用する(ブースト状況が分かりやすいため)。
        var maxClockMHz = cpu.Sensors
            .Where(s => s.SensorType == SensorType.Clock && s.Value.HasValue)
            .Select(s => (double)s.Value!.Value)
            .DefaultIfEmpty()
            .Max();

        double? clockGHz = maxClockMHz > 0 ? maxClockMHz / 1000.0 : null;

        return new CpuMetrics(totalLoad?.Value, clockGHz, cpu.Name);
    }
}
