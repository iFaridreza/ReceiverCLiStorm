namespace ReceiverCliStorm.TelegramBot.WTelegramUtils;

public interface IWTelegramManagerFactory
{
    IWTelegramManager Create(string apiId, string apiHash, string deviceModel, string systemVersion, string appVersion,
        string langCode, string sessionPath, Action<int, string>? logging);
}