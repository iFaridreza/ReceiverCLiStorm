namespace RecevicerCliStorm.TelegramBot.WTelegramClientUtils;

public class WTelegramClientManagerFactory : IWTelegramClientManagerFactory
{
    public IWTelegramClientManager Create(string apiId, string apiHash, string sessionPath, Action<int, string>? loging)
    {
        return new WTelegramClientManager(apiId, apiHash, sessionPath, loging);
    }
}
