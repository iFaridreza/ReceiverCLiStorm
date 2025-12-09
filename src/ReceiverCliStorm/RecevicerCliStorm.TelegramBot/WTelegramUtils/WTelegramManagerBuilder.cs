namespace ReceiverCliStorm.TelegramBot.WTelegramUtils;

public class WTelegramManagerBuilder : IWTelegramManagerBuilder
{
    private readonly WTelegramOptions _wTelegramOptions;

    public WTelegramManagerBuilder(WTelegramOptions wTelegramOptions)
    {
        _wTelegramOptions = wTelegramOptions;
    }

    public WTelegramManagerBuilder WithApiId(string apiId)
    {
        _wTelegramOptions.ApiId = apiId;
        return this;
    }

    public WTelegramManagerBuilder WithApiHash(string apiHash)
    {
        _wTelegramOptions.ApiHash = apiHash;
        return this;
    }

    public WTelegramManagerBuilder WithDeviceModel(string deviceModel)
    {
        _wTelegramOptions.DeviceModel = deviceModel;
        return this;
    }

    public WTelegramManagerBuilder WithSystemVersion(string systemVersion)
    {
        _wTelegramOptions.SystemVersion = systemVersion;
        return this;
    }

    public WTelegramManagerBuilder WithAppVersion(string appVersion)
    {
        _wTelegramOptions.AppVersion = appVersion;
        return this;
    }

    public WTelegramManagerBuilder WithLangCode(string langCode)
    {
        _wTelegramOptions.LangCode = langCode;
        return this;
    }

    public WTelegramManagerBuilder WithSessionPath(string sessionPath)
    {
        _wTelegramOptions.SessionPath = sessionPath;
        return this;
    }

    public WTelegramManagerBuilder WithLoging(Action<int, string> loging)
    {
        _wTelegramOptions.Loging = loging;
        return this;
    }

    public IWTelegramManager Build()
    {
        return new WTelegramManager(_wTelegramOptions);
    }
}
