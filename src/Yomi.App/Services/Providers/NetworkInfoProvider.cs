using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Yomi.App.Services.Providers;

/// <summary>
/// リンクアップ済みでデフォルトゲートウェイを持つ主要アダプター1つの
/// IPv4アドレス(プレフィックス付き)とDNSサーバーを取得する。
/// </summary>
public sealed class NetworkInfoProvider : INetworkInfoProvider
{
    private long? _lastBytesSent;
    private long? _lastBytesReceived;
    private DateTime? _lastSampleTime;

    public NetworkInfo GetNetworkInfo()
    {
        var nic = GetPrimaryInterface();
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

    public NetworkSpeed GetSpeed()
    {
        var nic = GetPrimaryInterface();
        if (nic is null)
        {
            _lastBytesSent = null;
            _lastBytesReceived = null;
            _lastSampleTime = null;
            return new NetworkSpeed(0, 0);
        }

        var stats = nic.GetIPv4Statistics();
        var now = DateTime.UtcNow;

        NetworkSpeed speed = default;
        if (_lastSampleTime is { } lastTime && _lastBytesSent is { } lastSent && _lastBytesReceived is { } lastReceived)
        {
            var elapsedSeconds = (now - lastTime).TotalSeconds;
            if (elapsedSeconds > 0)
            {
                // バイト/秒 → Mbps (1 byte = 8 bit, 1 Mbps = 1,000,000 bit/s)
                var uploadMbps = Math.Max(0, stats.BytesSent - lastSent) * 8.0 / elapsedSeconds / 1_000_000.0;
                var downloadMbps = Math.Max(0, stats.BytesReceived - lastReceived) * 8.0 / elapsedSeconds / 1_000_000.0;
                speed = new NetworkSpeed(uploadMbps, downloadMbps);
            }
        }

        _lastBytesSent = stats.BytesSent;
        _lastBytesReceived = stats.BytesReceived;
        _lastSampleTime = now;

        return speed;
    }

    private static NetworkInterface? GetPrimaryInterface() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Where(n => n.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
            .FirstOrDefault();
}
