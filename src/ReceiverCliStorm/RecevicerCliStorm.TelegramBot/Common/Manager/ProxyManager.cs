using RecevicerCliStorm.TelegramBot.Common.Dto;
using Starksoft.Net.Proxy;
using System.Text.Json;

namespace RecevicerCliStorm.TelegramBot.Common.Manager;

public class ProxyManager
{
    static IEnumerable<Proxy> Proxies { get; set; }

    static ProxyManager()
    {
        Proxies = new List<Proxy>();
    }

    public static bool HaveData(string proxyPath)
    {
        string data = File.ReadAllText(proxyPath);

        return !string.IsNullOrEmpty(data);
    }

    public static void SetProxy(string proxyPath)
    {
        string data = File.ReadAllText(proxyPath);

        IEnumerable<Proxy>? proxys = JsonSerializer.Deserialize<IEnumerable<Proxy>>(data);

        Proxies = proxys ?? throw new NullReferenceException(nameof(proxyPath));
    }

    public static Proxy RandomProxy()
    {
        if (Proxies is null || !Proxies.Any())
        {
            throw new NullReferenceException(nameof(Proxies));
        }

        return Proxies.ElementAt(new Random().Next(0, Proxies.Count()));
    }

    public static bool IsConnectSocks5Proxy(string proxyHost, int proxyPort, string? username = null, string? password = null)
    {
        try
        {
            var proxy = new Socks5ProxyClient(proxyHost, proxyPort, username, password);
            using var stream = proxy.CreateConnection("core.telegram.org", 443);
            return stream != null && stream.Connected;
        }
        catch
        {
            return false;
        }
    }
}