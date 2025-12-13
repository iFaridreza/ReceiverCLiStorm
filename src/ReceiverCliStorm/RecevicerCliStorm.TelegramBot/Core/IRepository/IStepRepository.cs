using ReceiverCliStorm.TelegramBot.Core.Domain;

namespace ReceiverCliStorm.TelegramBot.Core.IRepository;

public interface IStepRepository
{
    Task Create(Step step);
    Task<bool> Any(long chatId);
    Task<Step> Get(long chatId);
    Task Remove(long chatId);
    Task<IEnumerable<Step>> GetAll();
}