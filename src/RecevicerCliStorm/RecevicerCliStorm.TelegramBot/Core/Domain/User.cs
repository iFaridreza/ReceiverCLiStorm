namespace RecevicerCliStorm.TelegramBot.Core.Domain;

public class User
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public bool IsBlock { get; set; }
    public ELanguage Language { get; set; }
}