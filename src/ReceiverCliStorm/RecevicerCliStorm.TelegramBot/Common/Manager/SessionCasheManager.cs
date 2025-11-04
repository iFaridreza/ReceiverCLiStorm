using ReceiverCliStorm.TelegramBot.Common.Dto;
using System.Collections.Concurrent;

namespace ReceiverCliStorm.TelegramBot.Common.Manager;

public static class SessionCasheManager
{
    private static ConcurrentDictionary<long, SessionCashe> keyValuePairs;

    static SessionCasheManager() => keyValuePairs = new();

    public static void AddOrUpdate(long chatId, SessionCashe sessionCashe)
    {
        keyValuePairs.AddOrUpdate(chatId,
            id => sessionCashe,
            (id, existing) => sessionCashe
        );
    }

    public static bool Any(long chatId) => keyValuePairs.ContainsKey(chatId);

    public static SessionCashe Get(long chatId)
    {
        keyValuePairs.TryGetValue(chatId, out SessionCashe? sessionCashe);

        if (sessionCashe is null)
        {
            throw new KeyNotFoundException($"Session Cashe Not Found In Cash Key {chatId}");
        }

        return sessionCashe;
    }

    public static void Remove(long chatId) => keyValuePairs.TryRemove(chatId, out _);
}
