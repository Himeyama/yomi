using Yomi.App.Interop;

namespace Yomi.App.Services.Providers;

/// <summary>
/// Win32 の GlobalMemoryStatusEx で物理メモリの使用状況を取得する。
/// カーネルドライバに依存しない軽量な取得方法。
/// </summary>
public sealed class MemoryMetricsProvider : IMemoryMetricsProvider
{
    private const double BytesPerGiB = 1024.0 * 1024.0 * 1024.0;

    public MemoryMetrics GetMetrics()
    {
        var status = new NativeMethods.MEMORYSTATUSEX
        {
            dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>(),
        };

        if (!NativeMethods.GlobalMemoryStatusEx(ref status))
        {
            return new MemoryMetrics(null, null, null);
        }

        return Compute(status.ullTotalPhys, status.ullAvailPhys);
    }

    /// <summary>総物理メモリと空き物理メモリ(バイト)から使用率・使用量・総量を求める。</summary>
    public static MemoryMetrics Compute(ulong totalBytes, ulong availableBytes)
    {
        if (totalBytes == 0) return new MemoryMetrics(null, null, null);

        var usedBytes = totalBytes - availableBytes;
        var usagePercent = (double)usedBytes / totalBytes * 100.0;
        var usedGiB = usedBytes / BytesPerGiB;
        var totalGiB = totalBytes / BytesPerGiB;

        return new MemoryMetrics(usagePercent, usedGiB, totalGiB);
    }
}
