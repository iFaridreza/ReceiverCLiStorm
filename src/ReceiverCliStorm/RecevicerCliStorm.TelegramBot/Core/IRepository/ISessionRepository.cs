using ReceiverCliStorm.TelegramBot.Core.Domain;

namespace ReceiverCliStorm.TelegramBot.Core.IRepository;

public interface ISessionRepository
{
    Task Create(Session session);
    Task<bool> Any(string countryCode, string number);
    Task UpdateStatus(Session session,ESessionStatus eSessionStatus);
    Task<Session> Get(string countryCode , string number);
    Task<IEnumerable<Session>> GetAll(long chatUserId);
    Task<IEnumerable<Session>> GetAll();
}
