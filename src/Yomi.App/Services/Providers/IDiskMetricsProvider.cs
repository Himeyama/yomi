namespace Yomi.App.Services.Providers;

public readonly record struct DiskDriveMetric(
    string Name,
    double UsagePercent,
    double UsedBytes,
    double TotalBytes);

public interface IDiskMetricsProvider
{
    IReadOnlyList<DiskDriveMetric> GetMetrics();
}
