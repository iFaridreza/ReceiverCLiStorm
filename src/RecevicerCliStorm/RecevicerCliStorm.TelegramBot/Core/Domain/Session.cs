namespace RecevicerCliStorm.TelegramBot.Core.Domain;

public class Session
{
    public long Id { get; set; }
    public required string CountryCode { get; set; }
    public required string Number { get; set; }
    public required ESessionStatus ESessionStatus { get; set; }
    public DateOnly RegisterDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public long UserId { get; set; }
    public required User User { get; set; }
    public long SessionInfoId { get; set; }
    public required SessionInfo SessionInfo { get; set; }
}
