namespace ReceiverCliStorm.TelegramBot.WTelegramUtils;

public class WTelegramOptions
{
    public string ApiId { get; set; } = string.Empty;
    public string ApiHash { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string SystemVersion { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string LangCode { get; set; } = string.Empty;
    public string SessionPath { get; set; } = string.Empty;

    public required Action<int, string> Loging;
}
