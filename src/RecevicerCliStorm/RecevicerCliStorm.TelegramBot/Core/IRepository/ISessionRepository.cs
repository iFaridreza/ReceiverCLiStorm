using RecevicerCliStorm.TelegramBot.Core.Domain;

namespace RecevicerCliStorm.TelegramBot.Core.IRepository;

public interface ISessionRepository
{
    Task Create(Session session);
    Task<bool> Any(string countryCode, string number);
    Task Update(Session session);
    Task Get(string countryCode , string number);
    Task<IEnumerable<Session>> GetAll();
}
