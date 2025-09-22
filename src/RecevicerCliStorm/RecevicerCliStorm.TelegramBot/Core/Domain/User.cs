namespace RecevicerCliStorm.TelegramBot.Core.Domain;

public class User
{
    public int Id { get; set; }
    public long ChatId { get; set; }
    public bool IsBlock { get; set; }
}