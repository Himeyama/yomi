using System.Diagnostics;
using Microsoft.Win32;

namespace Yomi.App.Services.Providers;

/// <summary>
/// Windows のパフォーマンスカウンターとレジストリのみで CPU メトリクスを取得する。
/// カーネルドライバ(WinRing0)にも WMI にも依存しない。
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
        // レジストリ参照はコンストラクタ(UIスレッド)ではなく、初回サンプリング時(タイマースレッド)に遅延実行する。
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

    /// <summary>
    /// CPU の名前とベースクロック(MHz)をレジストリから取得する。
    /// WMI(System.Management)を避けることで、WMI インフラの初期化コストと
    /// アセンブリのロードを回避する。
    /// - 名前: "ProcessorNameString"(REG_SZ)
    /// - ベースクロック: "~MHz"(REG_DWORD、定格クロックのMHz値)
    /// </summary>
    private static (string? Name, double? BaseClockMHz) QueryCpuInfo()
    {
        const string cpuKeyPath = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(cpuKeyPath);
            if (key is null) return (null, null);

            var name = (key.GetValue("ProcessorNameString") as string)?.Trim();
            var baseClock = key.GetValue("~MHz") is int mhz and > 0 ? (double?)mhz : null;
            return (name, baseClock);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            // レジストリが参照できない環境では名前・クロックなしで動作を継続する。
            return (null, null);
        }
    }

    public void Dispose()
    {
        _utilityCounter.Dispose();
        _perfCounter.Dispose();
    }
}
