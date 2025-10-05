using RecevicerCliStorm.TelegramBot.Core.Domain;

namespace RecevicerCliStorm.TelegramBot.Core.IRepository;

public interface ISudoRepository
{
    Task Create(Sudo sudo);
    Task<bool> Any(long chatId);
    Task<Sudo> Get(long chatId);
    Task<ELanguage> GetLanguage(long chatId);
    Task ChangeLanguage(Sudo sudo, ELanguage eLanguage);
}