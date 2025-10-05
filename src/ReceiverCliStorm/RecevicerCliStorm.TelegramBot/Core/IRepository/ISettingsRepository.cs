using RecevicerCliStorm.TelegramBot.Core.Domain;

namespace RecevicerCliStorm.TelegramBot.Core.IRepository;

public interface ISettingsRepository
{
    Task Create(Settings settings);
    Task<bool> Any();
    Task<Settings> GetSingleFirst();
    Task Update(Settings settings);
}
