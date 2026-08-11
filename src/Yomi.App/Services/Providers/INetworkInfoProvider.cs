using System.Net.NetworkInformation;

namespace Yomi.App.Services.Providers;

public readonly record struct NetworkInfo(string? IpAddressWithPrefix, string? DnsServers);

public readonly record struct NetworkSpeed(double UploadMbps, double DownloadMbps);

public interface INetworkInfoProvider
{
    /// <summary>
    /// 主要ネットワークアダプター1つを解決する。全NIC列挙を伴い比較的重いため、
    /// 1ティック内で一度だけ呼び、結果を各取得メソッドに渡して重複列挙を避ける。
    /// </summary>
    NetworkInterface? GetPrimaryInterface();

    NetworkInfo GetNetworkInfo(NetworkInterface? nic);

    /// <summary>前回呼び出しからの経過時間をもとに、直近の送受信速度をMbpsで返す。</summary>
    NetworkSpeed GetSpeed(NetworkInterface? nic);
}
