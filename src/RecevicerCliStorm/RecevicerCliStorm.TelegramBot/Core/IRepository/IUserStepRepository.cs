using RecevicerCliStorm.TelegramBot.Core.Domain;

namespace RecevicerCliStorm.TelegramBot.Core.IRepository;

public interface IUserStepRepository
{
    Task Create(UserStep userStep);
    Task<bool> Any(long chatId);
    Task<UserStep> Get(long chatId);
    Task Update(UserStep userStep);
    Task Remove(long chatId);
    Task<IEnumerable<UserStep>> GetAll();
}
