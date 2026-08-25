using System.Diagnostics;

namespace Yomi.App.Services.Providers;

/// <summary>
/// Windows のパフォーマンスカウンターと DXGI のみで GPU メトリクスを取得する。
/// カーネルドライバ(WinRing0)に依存しない。
///
/// - 使用率: "GPU Engine" カテゴリの 3D エンジン使用率を合算(タスクマネージャーと同方式)。
/// - VRAM使用量・総量・名前: DXGI (IDXGIFactory1::EnumAdapters1) で取得したアダプター一覧と、
///   "GPU Adapter Memory" の Dedicated Usage を AdapterLuid で突き合わせる。
///   レジストリの qwMemorySize は LUID を持たず、Dedicated Usage 最大のアダプターと
///   総量最大のアダプターが食い違う(複数GPU環境で総量が誤表示される)ことがあるため使わない。
/// </summary>
public sealed class GpuMetricsProvider : IGpuMetricsProvider
{
    private const string GpuEngineCategory = "GPU Engine";
    private const string GpuAdapterMemoryCategory = "GPU Adapter Memory";
    private const string UtilizationCounter = "Utilization Percentage";
    private const string DedicatedUsageCounter = "Dedicated Usage";
    private const double BytesPerGiB = 1024.0 * 1024.0 * 1024.0;

    // Utilization Percentage は 100ns 単位の累積稼働時間カウンター(PERF_100NSEC_TIMER)。
    // GPUプロセスのインスタンスは短命で GetCounters() 実行中に消えるため、ReadCategory() で
    // 全インスタンスを一括スナップショットし、前回との差分から使用率を算出する。
    private const double HundredNanosecondsPerSecond = 1e7;

    private readonly IReadOnlyList<DxgiAdapterInfo> _adapters = DxgiAdapterInfoProvider.QueryAdapters();
    private Dictionary<string, long>? _lastUtilizationRaw;
    private long _lastTimestamp;

    public GpuMetrics GetMetrics()
    {
        var usage = ReadGpuUtilization();
        var (vramUsedBytes, adapter) = ReadMaxDedicatedUsage(_adapters);

        double? vramUsedGiB = vramUsedBytes.HasValue ? vramUsedBytes.Value / BytesPerGiB : null;
        double? vramTotalGiB = adapter?.VramTotalGiB;
        double? vramUsagePercent = ComputeVramUsagePercent(vramUsedGiB, vramTotalGiB);

        return new GpuMetrics(usage, vramUsagePercent, vramUsedGiB, vramTotalGiB, adapter?.Name);
    }

    /// <summary>VRAM使用量(GiB)と総量(GiB)から使用率(%)を求める。総量が0/未取得なら null。</summary>
    public static double? ComputeVramUsagePercent(double? usedGiB, double? totalGiB)
    {
        if (usedGiB is null || totalGiB is not > 0) return null;
        return usedGiB.Value / totalGiB.Value * 100.0;
    }

    /// <summary>
    /// GPU使用率を求める。タスクマネージャーと同様、エンジンタイプ(3D / VideoDecode / Compute 等)
    /// ごとに使用率を合算し、その最大値を「GPU使用率」とする。前回サンプルが無い初回は null。
    /// </summary>
    private double? ReadGpuUtilization()
    {
        try
        {
            if (!PerformanceCounterCategory.Exists(GpuEngineCategory)) return null;

            var category = new PerformanceCounterCategory(GpuEngineCategory);
            var utilization = category.ReadCategory()[UtilizationCounter];
            if (utilization is null) return null;

            var timestamp = Stopwatch.GetTimestamp();
            var currentRaw = new Dictionary<string, long>(utilization.Count);
            foreach (string instanceName in utilization.Keys)
            {
                currentRaw[instanceName] = utilization[instanceName].RawValue;
            }

            var previousRaw = _lastUtilizationRaw;
            var previousTimestamp = _lastTimestamp;
            _lastUtilizationRaw = currentRaw;
            _lastTimestamp = timestamp;

            if (previousRaw is null) return null;

            var elapsedSeconds = (timestamp - previousTimestamp) / (double)Stopwatch.Frequency;
            if (elapsedSeconds <= 0) return null;

            var sumByEngineType = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var (instanceName, rawValue) in currentRaw)
            {
                var engineType = ExtractEngineType(instanceName);
                if (engineType is null) continue;
                if (!previousRaw.TryGetValue(instanceName, out var prevValue)) continue;

                var busySeconds = (rawValue - prevValue) / HundredNanosecondsPerSecond;
                if (busySeconds <= 0) continue;

                var percent = busySeconds / elapsedSeconds * 100.0;
                sumByEngineType.TryGetValue(engineType, out var current);
                sumByEngineType[engineType] = current + percent;
            }

            if (sumByEngineType.Count == 0) return 0.0;
            return Math.Min(sumByEngineType.Values.Max(), 100.0);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>"pid_1234_luid_..._phys_0_eng_0_engtype_3D" から "3D" を取り出す。</summary>
    private static string? ExtractEngineType(string instanceName)
    {
        const string marker = "engtype_";
        var index = instanceName.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? instanceName[(index + marker.Length)..] : null;
    }

    /// <summary>
    /// "GPU Adapter Memory" の Dedicated Usage が最大のインスタンスを選び、
    /// そのインスタンス名(luid_0x{high}_0x{low}_phys_0)から LUID を抽出して
    /// DXGI で取得済みのアダプター一覧と突き合わせる。
    /// 一致するアダプターが無ければ使用量のみ返す(総量・名前は null)。
    /// </summary>
    private static (double? UsedBytes, DxgiAdapterInfo? Adapter) ReadMaxDedicatedUsage(
        IReadOnlyList<DxgiAdapterInfo> adapters)
    {
        try
        {
            if (!PerformanceCounterCategory.Exists(GpuAdapterMemoryCategory)) return (null, null);

            var category = new PerformanceCounterCategory(GpuAdapterMemoryCategory);
            var dedicated = category.ReadCategory()[DedicatedUsageCounter];
            if (dedicated is null) return (null, null);

            double? maxValue = null;
            long? maxLuid = null;
            foreach (InstanceData data in dedicated.Values)
            {
                var value = data.RawValue;
                if (maxValue is null || value > maxValue)
                {
                    maxValue = value;
                    maxLuid = ExtractLuid(data.InstanceName);
                }
            }

            if (maxValue is null) return (null, null);

            DxgiAdapterInfo? adapter = null;
            if (maxLuid.HasValue)
            {
                foreach (var candidate in adapters)
                {
                    if (candidate.Luid != maxLuid.Value) continue;
                    adapter = candidate;
                    break;
                }
            }

            return (maxValue, adapter);
        }
        catch (InvalidOperationException)
        {
            return (null, null);
        }
    }

    private static readonly System.Text.RegularExpressions.Regex LuidPattern =
        new(@"luid_0x([0-9a-fA-F]+)_0x([0-9a-fA-F]+)_phys_", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>"luid_0x00000000_0x0000c33f_phys_0" から LUID(long) を取り出す。</summary>
    private static long? ExtractLuid(string instanceName)
    {
        var match = LuidPattern.Match(instanceName);
        if (!match.Success) return null;
        if (!uint.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out var high))
        {
            return null;
        }
        if (!uint.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber, null, out var low))
        {
            return null;
        }

        return ((long)high << 32) | low;
    }
}
