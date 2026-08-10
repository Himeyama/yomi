using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Yomi.App.Services.Providers;

/// <summary>
/// リンクアップ済みでデフォルトゲートウェイを持つ主要アダプター1つの
/// IPv4アドレス(プレフィックス付き)とDNSサーバーを取得する。
/// </summary>
public sealed class NetworkInfoProvider : INetworkInfoProvider
{
    public NetworkInfo GetNetworkInfo()
    {
        var nic = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Where(n => n.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
            .FirstOrDefault();

        if (nic is null) return new NetworkInfo(null, null);

        var props = nic.GetIPProperties();
        var ipv4 = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
        var ipWithPrefix = ipv4 is not null
            ? $"{ipv4.Address}/{ipv4.PrefixLength}"
            : null;

        var dns = props.DnsAddresses
            .Where(d => d.AddressFamily == AddressFamily.InterNetwork)
            .Select(d => d.ToString());
        var dnsJoined = string.Join(", ", dns);

        return new NetworkInfo(ipWithPrefix, string.IsNullOrEmpty(dnsJoined) ? null : dnsJoined);
    }
}
