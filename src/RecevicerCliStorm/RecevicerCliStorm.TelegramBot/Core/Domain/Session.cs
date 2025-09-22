namespace RecevicerCliStorm.TelegramBot.Core.Domain;

public class Session
{
    public int Id { get; set; }
    public int CountryCode { get; set; }
    public required string Number { get; set; }
    public required ESessionStatus ESessionStatus { get; set; }
    public DateOnly RegisterDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public int UserId { get; set; }
    public required User User { get; set; }
    public int SessionInfoId { get; set; }
    public required SessionInfo SessionInfo { get; set; }
}
