namespace ReceiverCliStorm.TelegramBot.Core.Domain;

public class Step
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public required string State { get; set; }
    public DateTime ExpierDateTime { get; set; }
}
