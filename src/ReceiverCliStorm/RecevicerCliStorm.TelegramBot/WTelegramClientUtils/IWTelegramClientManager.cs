namespace RecevicerCliStorm.TelegramBot.WTelegramClientUtils;

public interface IWTelegramClientManager
{
    Task Connect();
    Task Disconnect();
    Task Logout();
    Task<string> Login(string state);
    Task<int> ActiveSessionCount();
    Task<string[]> GetMessagesText(string contactPhone, int limit);
    Task<bool> IsReport();
    Task ResetSessions();
    Task UpdateInformashion(string firstName, string lastName, string? bio);
    Task UpdateProfilePhoto(string photoPath);
    Task ChangeStatus(bool online = true);
    void DisableUpdate(bool disable = true);
    void UseScokcs5Proxy(string host, int port, string username, string password);
    Task EnablePassword2Fa(string newPassword, string? hint);
    Task DisablePassword2Fa(string currentPassword);
    string GetSessionPath();
}
