using Starksoft.Net.Proxy;
using TL;
using WTelegram;

namespace ReceiverCliStorm.TelegramBot.WTelegramUtils;

public class WTelegramManager : IWTelegramManager
{
    private Client _client;
    private string _apiId;
    private string _apiHash;
    private string _sessionPath;


    public WTelegramManager(string apiId, string apiHash, string sessionPath, Action<int, string>? logging)
    {
        _apiId = apiId;
        _apiHash = apiHash;
        _sessionPath = sessionPath;
        Helpers.Log = logging;
        _client = new(Config);
    }

    private string? Config(string what)
    {
        string? result = what switch
        {
            "api_id" => _apiId,
            "api_hash" => _apiHash,
            "session_pathname" => _sessionPath,
            _ => null
        };

        return result;
    }

    public async Task Connect()
    {
        await _client.ConnectAsync(quickResume:true);
    }

    public async Task Disconnect()
    {
        await _client.DisposeAsync();
    }

    public async Task Logout()
    {
        await _client.Auth_LogOut();
    }

    public async Task<string> Login(string state)
    {
        string result = await _client.Login(state);
        return result;
    }

    public async Task<int> ActiveSessionCount()
    {
        Account_Authorizations authorizations = await _client.Account_GetAuthorizations();
        int count = authorizations.authorizations.Count();
        return count;
    }

    public async Task<string[]> GetMessagesText(string contactPhone, int limit)
    {
        Contacts_ResolvedPeer inputPeerUser = await _client.Contacts_ResolvePhone(contactPhone);
        Messages_MessagesBase resultHistory = await _client.Messages_GetHistory(inputPeerUser, limit: limit);

        ICollection<string> messages = new List<string>();

        foreach (var item in resultHistory.Messages)
        {
            Message? message = item as Message;

            if (message is null) continue;

            messages.Add(message.message);
        }

        return messages.ToArray();
    }

    public void Loging(Action<int, string> loging)
    {
        Helpers.Log = loging;
    }

    public void DisableUpdate(bool disable = true)
    {
        _client.DisableUpdates(disable);
    }

    public void UseScokcs5Proxy(string host, int port, string username, string password)
    {
        _client.TcpHandler = async (address, port443) =>
        {
            return await Task.Run(() =>
            {
                Socks5ProxyClient proxy = new Socks5ProxyClient(host, port, username, password);
                return proxy.CreateConnection(address, port443);
            });
        };
    }

    public async Task UpdateBio(string bio)
    {
        await _client.Account_UpdateProfile(about:bio);
    }

    public async Task UpdateProfilePhoto(string photoPath)
    {
        InputFileBase inputFile = await _client.UploadFileAsync(photoPath);
        await _client.Photos_UploadProfilePhoto(inputFile);
    }

    public async Task DisablePassword2Fa(string currentPassword)
    {
        Account_Password accountPwd = await _client.Account_GetPassword();

        InputCheckPasswordSRP? password = accountPwd.current_algo == null
            ? null
            : await Client.InputCheckPassword(accountPwd, currentPassword);

        accountPwd.current_algo = null;


        await _client.Account_UpdatePasswordSettings(password, new Account_PasswordInputSettings
        {
            flags = Account_PasswordInputSettings.Flags.has_new_algo,
            new_password_hash = null,
            new_algo = accountPwd.new_algo
        });
    }

    public async Task EnablePassword2Fa(string newPassword, string? hint)
    {
        Account_Password accountPwd = await _client.Account_GetPassword();

        InputCheckPasswordSRP? password = accountPwd.current_algo == null
            ? null
            : await Client.InputCheckPassword(accountPwd, newPassword);

        accountPwd.current_algo = null;

        var newPasswordHash = await WTelegram.Client.InputCheckPassword(accountPwd, newPassword);

        await _client.Account_UpdatePasswordSettings(password, new Account_PasswordInputSettings
        {
            flags = Account_PasswordInputSettings.Flags.has_new_algo,
            new_password_hash = newPasswordHash.A,
            new_algo = accountPwd.new_algo,
            hint = hint
        });
    }

    public async Task ChangeStatus(bool online = true)
    {
        await _client.Account_UpdateStatus(offline: online);
    }

    public async Task<bool> IsReport()
    {
        Contacts_ResolvedPeer inputPeerUser = await _client.Contacts_ResolveUsername("SpamBot");

        await _client.Contacts_Unblock(inputPeerUser);

        await _client.SendMessageAsync(inputPeerUser, "/start");
        Messages_MessagesBase resultHistory = await _client.Messages_GetHistory(inputPeerUser, limit: 1);

        if (resultHistory.Messages.Length < default(int))
        {
            return true;
        }

        Message message = (Message)resultHistory.Messages.Single();

        string lastMessage = message.message;

        return lastMessage.Contains("no limits") || lastMessage.Contains("Good news, no limits are currently applied to your account. You’re free as a bird!") ? false : true;
    }

    public async Task ResetSessions()
    {
        await _client.Auth_ResetAuthorizations();
    }

    public string GetSessionPath() => _sessionPath;
}