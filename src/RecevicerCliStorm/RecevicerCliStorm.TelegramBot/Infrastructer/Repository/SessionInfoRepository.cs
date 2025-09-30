using Microsoft.EntityFrameworkCore;
using RecevicerCliStorm.TelegramBot.Core.Domain;
using RecevicerCliStorm.TelegramBot.Core.IRepository;

namespace RecevicerCliStorm.TelegramBot.Infrastructer.Repository;

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
    }

    public async Task<SessionInfo> GetSingleFirst()
    {
        SessionInfo sessionInfo = await _context.SessionInfo.SingleAsync();
        return sessionInfo;
    }
}
