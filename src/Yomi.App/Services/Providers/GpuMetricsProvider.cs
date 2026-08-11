using System.Diagnostics;
using Microsoft.Win32;

namespace Yomi.App.Services.Providers;

/// <summary>
/// Windows のパフォーマンスカウンターとレジストリのみで GPU メトリクスを取得する。
/// カーネルドライバ(WinRing0)に依存しない。
///
/// - 使用率: "GPU Engine" カテゴリの 3D エンジン使用率を合算(タスクマネージャーと同方式)。
/// - VRAM使用量: "GPU Adapter Memory" の Dedicated Usage が最大のアダプターを採用。
/// - VRAM総量・名前: ディスプレイアダプターのレジストリ(qwMemorySize / DriverDesc)から取得。
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

    private readonly (string? Name, double? VramTotalGiB) _adapterInfo = QueryPrimaryAdapter();
    private Dictionary<string, long>? _lastUtilizationRaw;
    private long _lastTimestamp;

    public GpuMetrics GetMetrics()
    {
        var usage = ReadGpuUtilization();
        var vramUsedBytes = ReadMaxDedicatedUsageBytes();

        double? vramUsedGiB = vramUsedBytes.HasValue ? vramUsedBytes.Value / BytesPerGiB : null;
        double? vramUsagePercent = ComputeVramUsagePercent(vramUsedGiB, _adapterInfo.VramTotalGiB);

        return new GpuMetrics(usage, vramUsagePercent, vramUsedGiB, _adapterInfo.VramTotalGiB, _adapterInfo.Name);
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

    /// <summary>専用VRAM使用量が最大のアダプターのバイト数を返す。カウンターが無ければ null。</summary>
    private static double? ReadMaxDedicatedUsageBytes()
    {
        try
        {
            if (!PerformanceCounterCategory.Exists(GpuAdapterMemoryCategory)) return null;

            var category = new PerformanceCounterCategory(GpuAdapterMemoryCategory);
            var dedicated = category.ReadCategory()[DedicatedUsageCounter];
            if (dedicated is null) return null;

            double? max = null;
            foreach (InstanceData data in dedicated.Values)
            {
                var value = data.RawValue;
                if (max is null || value > max) max = value;
            }

            return max;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// ディスプレイアダプタークラスのレジストリから、専用VRAMが最大のアダプターの
    /// 名前(DriverDesc)と総VRAM量(qwMemorySize)を取得する。
    /// WMI の Win32_VideoController.AdapterRAM は 4GB 超で桁溢れするため使わない。
    /// </summary>
    private static (string? Name, double? VramTotalGiB) QueryPrimaryAdapter()
    {
        const string classKeyPath =
            @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

        try
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(classKeyPath);
            if (classKey is null) return (null, null);

            string? bestName = null;
            double? bestVramGiB = null;

            foreach (var subName in classKey.GetSubKeyNames())
            {
                // 数字4桁("0000"等)がアダプターインスタンス。それ以外(Properties等)は除外。
                if (subName.Length != 4 || !subName.All(char.IsDigit)) continue;

                using var adapterKey = classKey.OpenSubKey(subName);
                if (adapterKey?.GetValue("HardwareInformation.qwMemorySize") is not long memSize || memSize <= 0)
                {
                    continue;
                }

                var vramGiB = memSize / BytesPerGiB;
                if (bestVramGiB is null || vramGiB > bestVramGiB)
                {
                    bestVramGiB = vramGiB;
                    bestName = adapterKey.GetValue("DriverDesc") as string;
                }
            }

            return (bestName, bestVramGiB);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return (null, null);
        }
    }
}
