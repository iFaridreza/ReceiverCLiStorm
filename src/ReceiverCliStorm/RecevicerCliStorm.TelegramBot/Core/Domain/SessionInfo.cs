namespace RecevicerCliStorm.TelegramBot.Core.Domain;

public class SessionInfo
{
    public long Id { get; set; }
    public required string ApiId { get; set; }
    public required string ApiHash { get; set; }
}