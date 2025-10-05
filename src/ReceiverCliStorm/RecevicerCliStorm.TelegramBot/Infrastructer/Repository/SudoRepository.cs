using Microsoft.EntityFrameworkCore;
using RecevicerCliStorm.TelegramBot.Core.Domain;
using RecevicerCliStorm.TelegramBot.Core.IRepository;

namespace RecevicerCliStorm.TelegramBot.Infrastructer.Repository;

public class SudoRepository : ISudoRepository
{
    private readonly Context _context;

    public SudoRepository(Context context)
    {
        _context = context;
    }

    public async Task<bool> Any(long chatId)
    {
        bool any = await _context.Sudo.AnyAsync(x => x.ChatId == chatId);
        return any;
    }

    public async Task ChangeLanguage(Sudo sudo, ELanguage eLanguage)
    {
        sudo.Language = eLanguage;
        _context.Sudo.Update(sudo);
        await _context.SaveChangesAsync();
    }

    public async Task Create(Sudo sudo)
    {
        await _context.Sudo.AddAsync(sudo);
        await _context.SaveChangesAsync();
    }

    public async Task<Sudo> Get(long chatId)
    {
        Sudo sudo = await _context.Sudo.SingleAsync(x => x.ChatId == chatId);
        return sudo;
    }

    public async Task<ELanguage> GetLanguage(long chatId)
    {
        Sudo sudo = await Get(chatId);
        return sudo.Language;
    }
}