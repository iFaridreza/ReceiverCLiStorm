namespace ReceiverCliStorm.TelegramBot.WTelegramClientUtils;

public interface IWTelegramClientManagerFactory
{
    IWTelegramClientManager Create(string apiId, string apiHash, string sessionPath, Action<int, string>? loging);
}