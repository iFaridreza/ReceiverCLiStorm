using RecevicerCliStorm.TelegramBot.Core.Domain;

namespace RecevicerCliStorm.TelegramBot.Core.IRepository;

public interface IUserRepository
{
    Task Create(User user);
    Task<bool> Any(long chatId);
    Task<bool> IsBlock(long chatId);
    Task Update(User user);
}