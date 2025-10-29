using Microsoft.EntityFrameworkCore;
using ReceiverCliStorm.TelegramBot.Core.Domain;
using ReceiverCliStorm.TelegramBot.Core.IRepository;

namespace ReceiverCliStorm.TelegramBot.Infrastructer.Repository;

public class SettingsRepository : ISettingsRepository
{
    private readonly Context _context;

    public SettingsRepository(Context context)
    {
        _context = context;
    }

    public async Task<bool> Any()
    {
        bool any = await _context.Settings.AnyAsync();
        return any;
    }

    public async Task Create(Settings settings)
    {
        await _context.Settings.AddAsync(settings);
        await _context.SaveChangesAsync();
    }

    public async Task<Settings> GetSingleFirst()
    {
        Settings settings = await _context.Settings.SingleAsync();
        return settings;
    }

    public async Task Update(Settings settings)
    {
        _context.Settings.Update(settings);
        await _context.SaveChangesAsync();
    }
}
