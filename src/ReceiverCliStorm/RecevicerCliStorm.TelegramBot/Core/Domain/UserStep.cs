namespace ReceiverCliStorm.TelegramBot.Core.Domain;

public class UserStep
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public required string Step { get; set; }
    public DateTime ExpierDateTime { get; set; }
}
