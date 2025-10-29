namespace ReceiverCliStorm.TelegramBot.Core.Domain;

public class User
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public bool IsPermissionToUse { get; set; }
    public ELanguage Language { get; set; }
}