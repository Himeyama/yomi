using System.Diagnostics;
using System.Management;

namespace Yomi.App.Services.Providers;

/// <summary>
/// Windows のパフォーマンスカウンターと WMI のみで CPU メトリクスを取得する。
/// カーネルドライバ(WinRing0)に依存しないため、ウイルス対策ソフトの脆弱ドライバ検知を受けない。
/// </summary>
public sealed class CpuMetricsProvider : ICpuMetricsProvider, IDisposable
{
    // "% Processor Utility" はブースト込みの実効負荷を表し、タスクマネージャーの表示と一致する。
    private readonly PerformanceCounter _utilityCounter =
        new("Processor Information", "% Processor Utility", "_Total");

    // ベースクロックに対する現在の実効性能比(%)。ブースト時は100を超える。
    private readonly PerformanceCounter _perfCounter =
        new("Processor Information", "% Processor Performance", "_Total");

    private double? _baseClockMHz;
    private string? _name;
    private bool _cpuInfoQueried;

    public CpuMetrics GetMetrics()
    {
        // WMI クエリはコンストラクタ(UIスレッド)ではなく、初回サンプリング時(タイマースレッド)に遅延実行する。
        if (!_cpuInfoQueried)
        {
            (_name, _baseClockMHz) = QueryCpuInfo();
            _cpuInfoQueried = true;
        }

        var usage = (double)_utilityCounter.NextValue();
        var performanceRatio = (double)_perfCounter.NextValue();

        double? clockGHz = ComputeClockGHz(_baseClockMHz, performanceRatio);

        return new CpuMetrics(usage, clockGHz, _name);
    }

    /// <summary>ベースクロックと性能比(%)から実効クロック(GHz)を求める。取得不能な入力なら null。</summary>
    public static double? ComputeClockGHz(double? baseClockMHz, double performanceRatio)
    {
        if (baseClockMHz is not > 0 || performanceRatio <= 0) return null;
        return baseClockMHz.Value * performanceRatio / 100.0 / 1000.0;
    }

    private static (string? Name, double? BaseClockMHz) QueryCpuInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, MaxClockSpeed FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                var name = (obj["Name"] as string)?.Trim();
                var baseClock = obj["MaxClockSpeed"] is uint mhz ? (double?)mhz : null;
                return (name, baseClock);
            }
        }
        catch (ManagementException)
        {
            // WMI が利用できない環境では名前・クロックなしで動作を継続する。
        }

        return (null, null);
    }

    public void Dispose()
    {
        _utilityCounter.Dispose();
        _perfCounter.Dispose();
    }
}
