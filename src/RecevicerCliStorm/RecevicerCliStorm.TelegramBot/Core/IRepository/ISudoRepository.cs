using RecevicerCliStorm.TelegramBot.Core.Domain;

namespace RecevicerCliStorm.TelegramBot.Core.IRepository;

public interface ISudoRepository
{
    Task Create(Sudo sudo);
    Task<bool> Any(long chatId);
    Task Delete(long chatId);
    Task Update(Sudo sudo);
}
