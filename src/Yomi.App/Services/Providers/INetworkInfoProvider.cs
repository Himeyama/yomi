namespace Yomi.App.Services.Providers;

public readonly record struct NetworkInfo(string? IpAddressWithPrefix, string? DnsServers);

public interface INetworkInfoProvider
{
    NetworkInfo GetNetworkInfo();
}
