using Yomi.App.Models;
using Yomi.App.Services.Providers;

namespace Yomi.App.Services;

/// <summary>
/// 1秒間隔でハードウェアセンサーを更新し、MetricSampleを配信する。
/// UIスレッドをブロックしないよう独立したタイマースレッドで動作する。
/// </summary>
public sealed class SamplingService : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly ICpuMetricsProvider _cpuProvider;
    private readonly IMemoryMetricsProvider _memoryProvider;
    private readonly IGpuMetricsProvider _gpuProvider;
    private readonly IDiskMetricsProvider _diskProvider;
    private readonly INetworkInfoProvider _networkInfoProvider;
    private readonly Timer _timer;

    public event Action<MetricSample>? SampleUpdated;

    public SamplingService(
        HardwareMonitorService hardwareMonitor,
        ICpuMetricsProvider cpuProvider,
        IMemoryMetricsProvider memoryProvider,
        IGpuMetricsProvider gpuProvider,
        IDiskMetricsProvider diskProvider,
        INetworkInfoProvider networkInfoProvider)
    {
        _hardwareMonitor = hardwareMonitor;
        _cpuProvider = cpuProvider;
        _memoryProvider = memoryProvider;
        _gpuProvider = gpuProvider;
        _diskProvider = diskProvider;
        _networkInfoProvider = networkInfoProvider;
        _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        _hardwareMonitor.Open();
        _timer.Change(TimeSpan.Zero, Interval);
    }

    private const int NetworkInfoRefreshEveryNTicks = 5;
    private int _tickCount;
    private NetworkInfo _lastNetworkInfo;

    private void OnTick(object? state)
    {
        _hardwareMonitor.Update();

        var hardware = _hardwareMonitor.Hardware;
        var cpu = _cpuProvider.GetMetrics(hardware);
        var memory = _memoryProvider.GetMetrics(hardware);
        var gpu = _gpuProvider.GetMetrics(hardware);
        var disks = _diskProvider.GetMetrics();
        var speed = _networkInfoProvider.GetSpeed();

        if (_tickCount % NetworkInfoRefreshEveryNTicks == 0)
        {
            _lastNetworkInfo = _networkInfoProvider.GetNetworkInfo();
        }
        _tickCount++;

        SampleUpdated?.Invoke(new MetricSample(
            cpu.UsagePercent, cpu.ClockGHz, cpu.Name,
            memory.UsagePercent, memory.UsedGiB, memory.TotalGiB,
            gpu.UsagePercent, gpu.Name,
            gpu.VramUsagePercent, gpu.VramUsedGiB, gpu.VramTotalGiB,
            disks,
            _lastNetworkInfo.IpAddressWithPrefix, _lastNetworkInfo.DnsServers,
            speed.UploadMbps, speed.DownloadMbps));
    }

    public void Dispose()
    {
        _timer.Dispose();
        _hardwareMonitor.Dispose();
    }
}
