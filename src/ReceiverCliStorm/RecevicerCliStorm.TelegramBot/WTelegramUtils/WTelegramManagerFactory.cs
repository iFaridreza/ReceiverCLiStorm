namespace ReceiverCliStorm.TelegramBot.WTelegramUtils;

public class WTelegramManagerFactory : IWTelegramManagerFactory
{
    public IWTelegramManager Create(string apiId, string apiHash, string deviceModel, string systemVersion, string appVersion,
        string langCode, string sessionPath, Action<int, string>? logging)
    {
        return new WTelegramManager(apiId, apiHash, deviceModel, systemVersion, appVersion, langCode, sessionPath, logging);
    }
}
