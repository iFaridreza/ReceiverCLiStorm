using ReceiverCliStorm.TelegramBot.Common.Dto;
using System.Collections.Concurrent;

namespace ReceiverCliStorm.TelegramBot.Common.Manager;

public static class SessionCashManager
{
    private static readonly ConcurrentDictionary<long, SessionCash> _keyValuePairs;

    static SessionCashManager() => _keyValuePairs = new();

    public static void AddOrUpdate(long chatId, SessionCash sessionCash)
    {
        _keyValuePairs.AddOrUpdate(chatId,
            _ => sessionCash,
            (_, _) => sessionCash
        );
    }

    public static bool Any(long chatId) => _keyValuePairs.ContainsKey(chatId);

    public static SessionCash Get(long chatId)
    {
        _keyValuePairs.TryGetValue(chatId, out SessionCash? sessionCashe);

        if (sessionCashe is null)
        {
            throw new KeyNotFoundException($"Session Cashe Not Found In Cash Key {chatId}");
        }

        return sessionCashe;
    }

    public static void Remove(long chatId) => _keyValuePairs.TryRemove(chatId, out _);
}
