namespace ReceiverCliStorm.TelegramBot.WTelegramUtils;

public class WTelegramManagerFactory : IWTelegramManagerFactory
{
    public IWTelegramManager Create(string apiId, string apiHash, string sessionPath, Action<int, string>? loging)
    {
        return new WTelegramManager(apiId, apiHash, sessionPath, loging);
    }
}
