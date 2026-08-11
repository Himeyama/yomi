namespace Yomi.App.Services.Providers;

public readonly record struct MemoryMetrics(double? UsagePercent, double? UsedGiB, double? TotalGiB);

public interface IMemoryMetricsProvider
{
    MemoryMetrics GetMetrics();
}
