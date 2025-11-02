using Starksoft.Net.Proxy;
using System.Text.Json;
using ReceiverCliStorm.TelegramBot.Common.Dto;

namespace ReceiverCliStorm.TelegramBot.Common.Manager;

public class ProxyManager
{
    private static IEnumerable<Proxy> _proxies { get; set; }

    static ProxyManager()
    {
        _proxies = new List<Proxy>();
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

        _proxies = proxys ?? throw new NullReferenceException(nameof(proxyPath));
    }

    public static void CleanCashe()
    {
      _proxies = [];
    }
    
    public static Proxy RandomProxy()
    {
        if (_proxies is null || !_proxies.Any())
        {
            throw new NullReferenceException(nameof(_proxies));
        }

        return _proxies.ElementAt(new Random().Next(0, _proxies.Count()));
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

    public static int GetCount() => _proxies.Count();
}