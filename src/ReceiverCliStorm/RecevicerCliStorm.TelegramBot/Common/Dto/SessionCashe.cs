using ReceiverCliStorm.TelegramBot.WTelegramClientUtils;

namespace ReceiverCliStorm.TelegramBot.Common.Dto;

public class SessionCashe
{
    public required string PhoneNumber { get; init; }
    public required string SessionPath { get; init; }
    public required InfoPhoneNumber InfoPhoneNumber { get; init; }
    public required IWTelegramClientManager WTelegramClientManager { get; init; }
}
