using ReceiverCliStorm.TelegramBot.WTelegramClientUtils;
using System.Collections.Concurrent;

namespace ReceiverCliStorm.TelegramBot.Common.Manager;

public static class SessionCasheManager
{
    private static ConcurrentDictionary<long, IWTelegramClientManager> keyValuePairs;

    static SessionCasheManager() => keyValuePairs = new();

    public static void AddOrUpdate(long chatId, IWTelegramClientManager clientManager)
    {
        keyValuePairs.AddOrUpdate(chatId,
            id => clientManager,
            (id, existing) => clientManager
        );
    }

    public static bool Any(long chatId) => keyValuePairs.ContainsKey(chatId);

    public static IWTelegramClientManager Get(long chatId)
    {
        keyValuePairs.TryGetValue(chatId, out IWTelegramClientManager? clientManager);

        if (clientManager is null)
        {
            throw new KeyNotFoundException($"Session Not Found In Cash Key {chatId}");
        }

        return clientManager;
    }

    public static void Remove(long chatId) => keyValuePairs.TryRemove(chatId, out _);
}
