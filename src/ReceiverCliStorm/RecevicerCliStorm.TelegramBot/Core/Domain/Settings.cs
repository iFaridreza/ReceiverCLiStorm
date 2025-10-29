namespace ReceiverCliStorm.TelegramBot.Core.Domain;

public class Settings
{
    public long Id { get; set; }
    public bool UseProxy { get; set; }
    public bool UseChangeBio { get; set; }
    public bool UseCheckReport { get; set; }
    public bool UseLogCLI { get; set; }
}