namespace Yomi.App.Services.Providers;

public readonly record struct GpuMetrics(
    double? UsagePercent,
    double? VramUsagePercent,
    double? VramUsedGiB,
    double? VramTotalGiB,
    string? Name);

public interface IGpuMetricsProvider
{
    GpuMetrics GetMetrics();
}
