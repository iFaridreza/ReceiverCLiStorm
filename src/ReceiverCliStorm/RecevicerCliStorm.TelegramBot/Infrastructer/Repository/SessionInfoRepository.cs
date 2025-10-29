using Microsoft.EntityFrameworkCore;
using ReceiverCliStorm.TelegramBot.Core.Domain;
using ReceiverCliStorm.TelegramBot.Core.IRepository;

namespace ReceiverCliStorm.TelegramBot.Infrastructer.Repository;

public class SessionInfoRepository : ISessionInfoRepository
{
    private readonly Context _context;

    public SessionInfoRepository(Context context)
    {
        _context = context;
    }

    public async Task<bool> Any()
    {
        bool any = await _context.SessionInfo.AnyAsync();
        return any;
    }

    public async Task Create(SessionInfo sessionInfo)
    {
        await _context.SessionInfo.AddAsync(sessionInfo);
        await _context.SaveChangesAsync();
    }

    public async Task<SessionInfo> GetSingleFirst()
    {
        SessionInfo sessionInfo = await _context.SessionInfo.SingleAsync();
        return sessionInfo;
    }
}
