namespace ReceiverCliStorm.TelegramBot.WTelegramUtils;

public interface IWTelegramManagerBuilder
{
    WTelegramManagerBuilder WithApiId(string apiId);
    WTelegramManagerBuilder WithApiHash(string apiHash);
    WTelegramManagerBuilder WithDeviceModel(string deviceModel);
    WTelegramManagerBuilder WithSystemVersion(string systemVersion);
    WTelegramManagerBuilder WithAppVersion(string appVersion);
    WTelegramManagerBuilder WithLangCode(string langCode);
    WTelegramManagerBuilder WithSessionPath(string sessionPath);
    WTelegramManagerBuilder WithLoging(Action<int, string> loging);
    IWTelegramManager Build();
}