namespace ReceiverCliStorm.TelegramBot.Core.Domain;

public class DeviceAuthInfo
{
    public  long Id { get; set; }
    public required string DeviceModel { get; set; }
    public required string SystemVersion { get; set; }
    public required string AppVersion { get; set; }
    public required string LangCode { get; set; }
    public long SessionId { get; set; }
}