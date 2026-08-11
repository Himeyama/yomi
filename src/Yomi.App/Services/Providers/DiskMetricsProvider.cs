using System.IO;

namespace Yomi.App.Services.Providers;

public sealed class DiskMetricsProvider : IDiskMetricsProvider
{
    public IReadOnlyList<DiskDriveMetric> GetMetrics()
    {
        var result = new List<DiskDriveMetric>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;

            var totalBytes = (double)drive.TotalSize;
            if (totalBytes <= 0) continue;

            var freeBytes = (double)drive.TotalFreeSpace;
            var usedBytes = totalBytes - freeBytes;
            var usagePercent = usedBytes / totalBytes * 100.0;

            result.Add(new DiskDriveMetric(drive.Name.TrimEnd('\\'), usagePercent, usedBytes, totalBytes));
        }

        return result;
    }
}
