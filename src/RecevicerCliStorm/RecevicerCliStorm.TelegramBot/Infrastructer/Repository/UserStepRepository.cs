using Microsoft.EntityFrameworkCore;
using RecevicerCliStorm.TelegramBot.Core.Domain;
using RecevicerCliStorm.TelegramBot.Core.IRepository;

namespace RecevicerCliStorm.TelegramBot.Infrastructer.Repository;

public class UserStepRepository : IUserStepRepository
{
    private readonly Context _context;

    public UserStepRepository(Context context)
    {
        _context = context;
    }

    public async Task<bool> Any(long chatId)
    {
        bool any = await _context.UserStep.AnyAsync(x => x.ChatId == chatId);
        return any;
    }

    public async Task Create(UserStep userStep)
    {
        await _context.UserStep.AddAsync(userStep);
        await _context.SaveChangesAsync();
    }

    public async Task<UserStep> Get(long chatId)
    {
        UserStep userStep = await _context.UserStep.SingleAsync(x => x.ChatId == chatId);
        return userStep;
    }

    public async Task<IEnumerable<UserStep>> GetAll()
    {
        IEnumerable<UserStep> userSteps = await _context.UserStep.ToListAsync();
        return userSteps;
    }

    public async Task Remove(long chatId)
    {
        UserStep userStep = await Get(chatId);
        _context.UserStep.Remove(userStep);
        await _context.SaveChangesAsync();
    }

    public async Task Update(UserStep userStep)
    {
        _context.UserStep.Update(userStep);
        await _context.SaveChangesAsync();
    }
}
