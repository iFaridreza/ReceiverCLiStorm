using ReceiverCliStorm.TelegramBot.Common.Dto;
using System.Collections.Concurrent;

namespace ReceiverCliStorm.TelegramBot.Common.Manager;

public static class SessionCashManager
{
    private static readonly ConcurrentDictionary<long, SessionCashe> _keyValuePairs;

    static SessionCashManager() => _keyValuePairs = new();

    public static void AddOrUpdate(long chatId, SessionCashe sessionCashe)
    {
        _keyValuePairs.AddOrUpdate(chatId,
            _ => sessionCashe,
            (_, _) => sessionCashe
        );
    }

    public static bool Any(long chatId) => _keyValuePairs.ContainsKey(chatId);

    public static SessionCashe Get(long chatId)
    {
        _keyValuePairs.TryGetValue(chatId, out SessionCashe? sessionCashe);

        if (sessionCashe is null)
        {
            throw new KeyNotFoundException($"Session Cashe Not Found In Cash Key {chatId}");
        }

        return sessionCashe;
    }

    public static void Remove(long chatId) => _keyValuePairs.TryRemove(chatId, out _);
}
