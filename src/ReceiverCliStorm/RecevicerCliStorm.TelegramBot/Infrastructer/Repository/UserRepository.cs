using Microsoft.EntityFrameworkCore;
using RecevicerCliStorm.TelegramBot.Core.Domain;
using RecevicerCliStorm.TelegramBot.Core.IRepository;

namespace RecevicerCliStorm.TelegramBot.Infrastructer.Repository;

public class UserRepository : IUserRepository
{
    private readonly Context _context;

    public UserRepository(Context context)
    {
        _context = context;
    }

    public async Task<bool> Any(long chatId)
    {
        bool any = await _context.User.AnyAsync(x => x.ChatId == chatId);
        return any;
    }

    public async Task AuthorizedPermisionToUse(User user)
    {
        user.IsPermissionToUse = true;
        _context.User.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task ChangeLanguage(User user, ELanguage eLanguage)
    {
        user.Language = eLanguage;
        _context.User.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task Create(User user)
    {
        await _context.User.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User> Get(long chatId)
    {
        User user = await _context.User.SingleAsync(x => x.ChatId == chatId);
        return user;
    }

    public async Task<ELanguage> GetLanguage(long chatId)
    {
        User user = await Get(chatId);
        return user.Language;
    }

    public async Task<bool> IsPermisionToUse(long chatId)
    {
        User user = await Get(chatId);
        return user.IsPermissionToUse;
    }

    public async Task UnauthorizedPermisionToUse(User user)
    {
        user.IsPermissionToUse = false;
        _context.User.Update(user);
        await _context.SaveChangesAsync();
    }
}