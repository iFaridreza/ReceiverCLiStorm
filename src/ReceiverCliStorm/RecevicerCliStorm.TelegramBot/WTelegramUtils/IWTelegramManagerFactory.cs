namespace ReceiverCliStorm.TelegramBot.WTelegramUtils;

public interface IWTelegramManagerFactory
{
    IWTelegramManager Create(string apiId, string apiHash, string sessionPath, Action<int, string>? loging);
}