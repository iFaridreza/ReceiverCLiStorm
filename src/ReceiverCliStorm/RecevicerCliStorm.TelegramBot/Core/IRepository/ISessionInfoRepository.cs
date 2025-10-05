using RecevicerCliStorm.TelegramBot.Core.Domain;

namespace RecevicerCliStorm.TelegramBot.Core.IRepository;

public interface ISessionInfoRepository
{
    Task Create(SessionInfo sessionInfo);
    Task<SessionInfo> GetSingleFirst();
    Task<bool> Any();
}
