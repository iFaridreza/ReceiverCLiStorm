namespace ReceiverCliStorm.TelegramBot.WTelegramUtils;

public interface IWTelegramManager
{
    Task Connect();
    Task Disconnect();
    Task Logout();
    Task<string> Login(string state);
    Task<bool> IsReport();
    Task UpdateBio(string bio);
    Task ChangeStatus(bool online = true);
    void DisableUpdate(bool disable = true);
    void UseScokcs5Proxy(string host, int port, string username, string password);
    Task EnablePassword2Fa(string newPassword, string? hint);
    Task DisablePassword2Fa(string currentPassword);
}
