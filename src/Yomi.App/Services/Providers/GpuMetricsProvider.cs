using LibreHardwareMonitor.Hardware;

namespace Yomi.App.Services.Providers;

/// <summary>
/// GPUベンダー・ドライババージョンによりセンサー名が揺れるため、
/// 優先順位付きの候補名リストで最初に見つかったセンサーを採用する。
/// </summary>
public sealed class GpuMetricsProvider : IGpuMetricsProvider
{
    private const double MegabytesPerGibibyte = 1024.0;

    private static readonly HardwareType[] GpuHardwareTypes =
    [
        HardwareType.GpuNvidia,
        HardwareType.GpuAmd,
        HardwareType.GpuIntel,
    ];

    private static readonly string[] UsageCandidates =
    [
        "GPU Core",
        "GPU Render/Compute",
        "D3D 3D",
    ];

    private static readonly string[] VramUsedCandidates =
    [
        "GPU Memory Used",
        "D3D Dedicated Memory Used",
    ];

    private static readonly string[] VramTotalCandidates =
    [
        "GPU Memory Total",
        "D3D Dedicated Memory Total",
    ];

    public GpuMetrics GetMetrics(IReadOnlyList<IHardware> hardware)
    {
        var gpu = hardware.FirstOrDefault(h => GpuHardwareTypes.Contains(h.HardwareType));
        if (gpu is null) return new GpuMetrics(null, null, null, null, null);

        var usage = FindByCandidates(gpu.Sensors, SensorType.Load, UsageCandidates)?.Value;

        var usedMb = FindByCandidates(gpu.Sensors, SensorType.SmallData, VramUsedCandidates)?.Value;
        var totalMb = FindByCandidates(gpu.Sensors, SensorType.SmallData, VramTotalCandidates)?.Value;

        double? vramUsagePercent = usedMb is not null && totalMb is not null && totalMb != 0
            ? usedMb / totalMb * 100.0
            : null;
        double? vramUsedGiB = usedMb / MegabytesPerGibibyte;
        double? vramTotalGiB = totalMb / MegabytesPerGibibyte;

        return new GpuMetrics(usage, vramUsagePercent, vramUsedGiB, vramTotalGiB, gpu.Name);
    }

    private static ISensor? FindByCandidates(ISensor[] sensors, SensorType type, string[] candidates)
    {
        var typed = sensors.Where(s => s.SensorType == type).ToList();
        foreach (var candidate in candidates)
        {
            var exact = typed.FirstOrDefault(s => s.Name == candidate);
            if (exact is not null) return exact;
        }
        foreach (var candidate in candidates)
        {
            var partial = typed.FirstOrDefault(s => s.Name.Contains(candidate, StringComparison.OrdinalIgnoreCase));
            if (partial is not null) return partial;
        }
        return null;
    }
}
