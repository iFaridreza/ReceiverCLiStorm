using RecevicerCliStorm.TelegramBot.Core.Domain;

namespace RecevicerCliStorm.TelegramBot.Core.IRepository;

public interface IUserRepository
{
    Task Create(User user);
    Task<bool> Any(long chatId);
    Task<bool> IsBlock(long chatId);
    Task<User> Get(long chatId);
    Task Block(User user);
    Task UnBlock(User user);
    Task<ELanguage> GetLanguage(long chatId);
    Task ChangeLanguage(User user, ELanguage eLanguage);
}