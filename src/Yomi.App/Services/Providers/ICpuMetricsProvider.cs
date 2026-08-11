namespace Yomi.App.Services.Providers;

public readonly record struct CpuMetrics(double? UsagePercent, double? ClockGHz, string? Name);

public interface ICpuMetricsProvider
{
    CpuMetrics GetMetrics();
}
