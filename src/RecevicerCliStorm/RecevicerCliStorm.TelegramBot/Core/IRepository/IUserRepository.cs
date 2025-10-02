using RecevicerCliStorm.TelegramBot.Core.Domain;

namespace RecevicerCliStorm.TelegramBot.Core.IRepository;

public interface IUserRepository
{
    Task Create(User user);
    Task<bool> Any(long chatId);
    Task<bool> IsPermisionToUse(long chatId);
    Task<User> Get(long chatId);
    Task AuthorizedPermisionToUse(User user);
    Task UnauthorizedPermisionToUse(User user);
    Task<ELanguage> GetLanguage(long chatId);
    Task ChangeLanguage(User user, ELanguage eLanguage);
}