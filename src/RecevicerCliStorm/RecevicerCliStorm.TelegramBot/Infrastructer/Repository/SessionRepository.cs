using Microsoft.EntityFrameworkCore;
using RecevicerCliStorm.TelegramBot.Core.Domain;
using RecevicerCliStorm.TelegramBot.Core.IRepository;

namespace RecevicerCliStorm.TelegramBot.Infrastructer.Repository;

public class SessionRepository : ISessionRepository
{
    private readonly Context _context;

    public SessionRepository(Context context)
    {
        _context = context;
    }

    public async Task<bool> Any(string countryCode, string number)
    {
        bool any = await _context.Session.AnyAsync(x => x.CountryCode == countryCode && x.Number == number);
        return any;
    }

    public async Task Create(Session session)
    {
        await _context.Session.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task Get(string countryCode, string number)
    {
        Session session = await _context.Session
            .Include(x => x.SessionInfo)
            .Include(x => x.User)
            .SingleAsync(x => x.CountryCode == countryCode && x.Number == number);
    }

    public async Task<IEnumerable<Session>> GetAll()
    {
        IEnumerable<Session> sessions = await _context.Session.ToListAsync();
        return sessions;
    }

    public async Task UpdateStatus(Session session, ESessionStatus eSessionStatus)
    {
        session.ESessionStatus = eSessionStatus;
        _context.Session.Update(session);
        await _context.SaveChangesAsync();
    }
}
