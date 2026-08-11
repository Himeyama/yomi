namespace Yomi.App.Services.Providers;

public readonly record struct NetworkInfo(string? IpAddressWithPrefix, string? DnsServers);

public readonly record struct NetworkSpeed(double UploadMbps, double DownloadMbps);

public interface INetworkInfoProvider
{
    NetworkInfo GetNetworkInfo();

    /// <summary>前回呼び出しからの経過時間をもとに、直近の送受信速度をMbpsで返す。</summary>
    NetworkSpeed GetSpeed();
}
