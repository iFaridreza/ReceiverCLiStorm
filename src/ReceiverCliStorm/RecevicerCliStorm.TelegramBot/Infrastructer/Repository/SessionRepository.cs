using Microsoft.EntityFrameworkCore;
using ReceiverCliStorm.TelegramBot.Core.Domain;
using ReceiverCliStorm.TelegramBot.Core.IRepository;

namespace ReceiverCliStorm.TelegramBot.Infrastructer.Repository;

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

    public async Task<Session> Get(string countryCode, string number)
    {
        Session session = await _context.Session
            .Include(x => x.SessionInfo)
            .Include(x => x.User)
            .Include(x => x.DeviceAuthInfo)
            .SingleAsync(x => x.CountryCode == countryCode && x.Number == number);
        return session;
    }

    public async Task<IEnumerable<Session>> GetAll(long chatUserId)
    {
        IEnumerable<Session> sessions = await _context.Session.Include(x => x.SessionInfo)
            .Include(x => x.User)
            .Include(x => x.DeviceAuthInfo)
            .Where(x => x.User.ChatId == chatUserId).ToListAsync();
        return sessions;
    }

    public async Task<IEnumerable<Session>> GetAll()
    {
        IEnumerable<Session> sessions =
            await _context.Session
                .Include(x => x.SessionInfo)
                .Include(x => x.User)
                .Include(x => x.DeviceAuthInfo)
                .ToListAsync();
        return sessions;
    }

    public async Task UpdateStatus(Session session, ESessionStatus sessionStatus)
    {
        session.SessionStatus = sessionStatus;
        _context.Session.Update(session);
        await _context.SaveChangesAsync();
    }
}