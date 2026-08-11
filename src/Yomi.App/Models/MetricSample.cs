using Yomi.App.Services.Providers;

namespace Yomi.App.Models;

/// <summary>1秒ごとのサンプリング結果。値が取得不能な場合は null。</summary>
public readonly record struct MetricSample(
    double? CpuUsagePercent,
    double? CpuClockGHz,
    string? CpuName,
    double? MemoryUsagePercent,
    double? MemoryUsedGiB,
    double? MemoryTotalGiB,
    double? GpuUsagePercent,
    string? GpuName,
    double? VramUsagePercent,
    double? VramUsedGiB,
    double? VramTotalGiB,
    IReadOnlyList<DiskDriveMetric> Disks,
    string? IpAddressWithPrefix,
    string? DnsServers);
