using Microsoft.EntityFrameworkCore;
using ReceiverCliStorm.TelegramBot.Core.Domain;
using ReceiverCliStorm.TelegramBot.Core.IRepository;

namespace ReceiverCliStorm.TelegramBot.Infrastructer.Repository;

public class StepRepository : IStepRepository
{
    private readonly Context _context;

    public StepRepository(Context context)
    {
        _context = context;
    }

    public async Task<bool> Any(long chatId)
    {
        bool any = await _context.Step.AnyAsync(x => x.ChatId == chatId);
        return any;
    }

    public async Task Create(Step step)
    {
        await _context.Step.AddAsync(step);
        await _context.SaveChangesAsync();
    }

    public async Task<Step> Get(long chatId)
    {
        Step step = await _context.Step.SingleAsync(x => x.ChatId == chatId);
        return step;
    }

    public async Task<IEnumerable<Step>> GetAll()
    {
        IEnumerable<Step> userSteps = await _context.Step.ToListAsync();
        return userSteps;
    }

    public async Task Remove(long chatId)
    {
        Step step = await Get(chatId);
        _context.Step.Remove(step);
        await _context.SaveChangesAsync();
    }
}
