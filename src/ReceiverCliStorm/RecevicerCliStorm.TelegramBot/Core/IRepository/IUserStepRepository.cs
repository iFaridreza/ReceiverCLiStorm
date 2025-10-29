using ReceiverCliStorm.TelegramBot.Core.Domain;

namespace ReceiverCliStorm.TelegramBot.Core.IRepository;

public interface IUserStepRepository
{
    Task Create(UserStep userStep);
    Task<bool> Any(long chatId);
    Task<UserStep> Get(long chatId);
    Task Remove(long chatId);
    Task<IEnumerable<UserStep>> GetAll();
}