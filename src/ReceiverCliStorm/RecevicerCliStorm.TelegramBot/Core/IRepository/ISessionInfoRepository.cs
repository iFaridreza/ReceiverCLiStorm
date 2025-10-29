using ReceiverCliStorm.TelegramBot.Core.Domain;

namespace ReceiverCliStorm.TelegramBot.Core.IRepository;

public interface ISessionInfoRepository
{
    Task Create(SessionInfo sessionInfo);
    Task<SessionInfo> GetSingleFirst();
    Task<bool> Any();
}
