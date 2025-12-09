using Microsoft.Extensions.DependencyInjection;
using ReceiverCliStorm.TelegramBot.Common;
using ReceiverCliStorm.TelegramBot.Common.Dto;
using ReceiverCliStorm.TelegramBot.Common.Manager;
using ReceiverCliStorm.TelegramBot.Core.Domain;
using ReceiverCliStorm.TelegramBot.Core.IRepository;
using ReceiverCliStorm.TelegramBot.WTelegramUtils;
using Serilog;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = ReceiverCliStorm.TelegramBot.Core.Domain.User;

namespace ReceiverCliStorm.TelegramBot.Bot;

public class TelegramBotApi : ITelegramBotApi
{
    private readonly TelegramBotClient _telegramBotClient;
    private readonly AppSettings _appSettings;
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public TelegramBotApi(TelegramBotClient telegramBotClient, AppSettings appSettings, ILogger logger,
        IServiceProvider serviceProvider)
    {
        _telegramBotClient = telegramBotClient;
        _appSettings = appSettings;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void Listen()
    {
        _telegramBotClient.OnMessage += OnMessage;
        _telegramBotClient.OnUpdate += OnUpdate;

        _logger.Information($"- Receiver TG Online");
    }

    public async Task OnMessage(Message message, UpdateType updateType)
    {
        _ = Task.Run(async () =>
        {
            if (message.Chat.Type is not ChatType.Private ||
                updateType is not UpdateType.Message ||
                message.Type is not MessageType.Text ||
                message.Text is null ||
                message.From is null)
            {
                return;
            }

            await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
            ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
            IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            IUserStepRepository userStepRepository = scope.ServiceProvider.GetRequiredService<IUserStepRepository>();

            long chatUserId = message.From.Id;
            int messageId = message.MessageId;

            string messageText = message.Text;

            await _telegramBotClient.SendChatAction(chatUserId, ChatAction.Typing);

            try
            {
                switch (messageText)
                {
                    case "/start":
                        {
                            await OnStart(chatUserId, messageId);
                        }
                        break;
                    case "/language":
                        {
                            await OnLanguage(chatUserId, messageId);
                        }
                        break;
                    case "/infouser":
                        {
                            await OnInfoUser(chatUserId, messageId);
                        }
                        break;
                    case "/cancel":
                        {
                            await OnCancel(chatUserId, messageId);
                        }
                        break;
                    case "/reload":
                        {
                            await OnReload(chatUserId, messageId);
                        }
                        break;
                    case "/settings":
                        {
                            await OnSettings(chatUserId, messageId);
                        }
                        break;
                    case "/help":
                        {
                            await OnHelp(chatUserId, messageId);
                        }
                        break;
                    default:
                        {
                            await OnStep(chatUserId, messageId, messageText);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(Exception));

                bool anyStep = await userStepRepository.Any(chatUserId);

                if (anyStep)
                {
                    await userStepRepository.Remove(chatUserId);
                }

                bool anyCashe = SessionCashManager.Any(chatUserId);

                if (anyCashe)
                {
                    SessionCash sessionCash = SessionCashManager.Get(chatUserId);

                    IWTelegramManager wTelegramClientManager = sessionCash.WTelegramManager;

                    await wTelegramClientManager.Disconnect();

                    SessionCashManager.Remove(chatUserId);
                }

                bool anySudo = await sudoRepository.Any(chatUserId);

                if (anySudo)
                {
                    ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

                    await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageSudo, "tryAgain"));

                    return;
                }

                bool anyUser = await userRepository.Any(chatUserId);

                if (!anyUser)
                {
                    await userRepository.Create(new()
                    {
                        ChatId = chatUserId,
                        Language = ELanguage.En
                    });

                    _logger.Information($"- User {chatUserId} signup to bot");
                }

                ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

                await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "tryAgain"));
            }
        });

        await Task.CompletedTask;
    }

    public async Task OnUpdate(Update update)
    {
        _ = Task.Run(async () =>
        {
            if (update.CallbackQuery is null ||
                update.CallbackQuery.Message is null ||
                update.CallbackQuery.Message.Chat.Type != ChatType.Private)
            {
                return;
            }

            await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
            ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
            IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            IUserStepRepository userStepRepository = scope.ServiceProvider.GetRequiredService<IUserStepRepository>();

            long chatUserId = update.CallbackQuery.Message.Chat.Id;
            int messageId = update.CallbackQuery.Message.MessageId;


            try
            {
                string? callbackData = update.CallbackQuery.Data;

                if (string.IsNullOrEmpty(callbackData))
                {
                    return;
                }

                switch (callbackData)
                {
                    case "IJoin":
                        {
                            await OnUpdateIJoin(chatUserId, messageId);
                        }
                        break;
                    case "Reload":
                        {
                            await OnUpdateReload(chatUserId, messageId);
                        }
                        break;
                    case "UseProxy":
                        {
                            await OnUpdateUseProxy(chatUserId, messageId);
                        }
                        break;
                    case $"UseChangeBio":
                        {
                            await OnUpdateUseChangeBio(chatUserId, messageId);
                        }
                        break;
                    case "UseLogCLi":
                        {
                            await OnUpdateUseLogCLi(chatUserId, messageId);
                        }
                        break;
                    case "UseCheckReport":
                        {
                            await OnUpdateUseCheckReport(chatUserId, messageId);
                        }
                        break;
                    default:
                        {
                            if (callbackData.Contains("ChangePermission_"))
                            {
                                await OnUpdateChangePermission(chatUserId, messageId, callbackData);
                            }
                            else if (callbackData.Contains("BackupSessions_"))
                            {
                                await OnUpdateBackupSessions(chatUserId, messageId, callbackData);
                            }
                            else if (callbackData.Contains("Download_"))
                            {
                                await OnUpdateDownloadCurrentSession(chatUserId, messageId, callbackData);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(Exception));

                bool anyStep = await userStepRepository.Any(chatUserId);

                if (anyStep)
                {
                    await userStepRepository.Remove(chatUserId);
                }

                bool anyCashe = SessionCashManager.Any(chatUserId);

                if (anyCashe)
                {
                    SessionCash sessionCash = SessionCashManager.Get(chatUserId);

                    IWTelegramManager wTelegramClientManager = sessionCash.WTelegramManager;

                    await wTelegramClientManager.Disconnect();

                    SessionCashManager.Remove(chatUserId);
                }

                bool anySudo = await sudoRepository.Any(chatUserId);

                if (anySudo)
                {
                    ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

                    await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageSudo, "tryAgain"));

                    return;
                }

                bool anyUser = await userRepository.Any(chatUserId);

                if (!anyUser)
                {
                    await userRepository.Create(new()
                    {
                        ChatId = chatUserId,
                        Language = ELanguage.En
                    });

                    _logger.Information($"- User {chatUserId} signup to bot");
                }

                ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

                await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "tryAgain"));
            }
        });

        await Task.CompletedTask;
    }

    private async Task OnUpdatePhone(long chatUserId, int messageId, string messageText)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        IUserStepRepository userStepRepository = scope.ServiceProvider.GetRequiredService<IUserStepRepository>();
        ISessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        ISessionInfoRepository sessionInfoRepository =
            scope.ServiceProvider.GetRequiredService<ISessionInfoRepository>();
        ISettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        IWTelegramManagerBuilder wTelegramManagerBuilder =
            scope.ServiceProvider.GetRequiredService<IWTelegramManagerBuilder>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (anySudo)
        {
            return;
        }

        bool anyUser = await userRepository.Any(chatUserId);

        if (!anyUser)
        {
            await userRepository.Create(new()
            {
                ChatId = chatUserId,
                Language = ELanguage.En
            });

            _logger.Information($"- User {chatUserId} signup to bot");
        }

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);
        Dictionary<string, string> usernameJoinForces = await IsJoinForce(chatUserId);

        if (usernameJoinForces.Count > default(int))
        {
            _logger.Information($"- User {chatUserId} show list to force join bot");
            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "joinRequest"),
                ParseMode.Html, replyParameters: messageId,
                replyMarkup: ReplyKeyboard.JoinForce(usernameJoinForces, Utils.GetText(eLanguageUser, "joined")));
            return;
        }

        bool isPermissionToUse = await IsPermissionToUse(chatUserId);

        if (!isPermissionToUse)
        {
            _logger.Information($"- User {chatUserId} access denied use a bot");
            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "acsessDenidUseBot"),
                ParseMode.Html, replyParameters: messageId,
                replyMarkup: ReplyKeyboard.Developer(Utils.GetText(eLanguageUser, "developer"),
                    _appSettings.Developer));
            return;
        }

        _logger.Information($"- User {chatUserId} Send {messageText} Bot");

        string phoneNumber = messageText.Replace(" ", string.Empty);

        bool isValidPhone = Utils.IsPhoneNumber(phoneNumber);

        if (!isValidPhone)
        {
            _logger.Information($"- User {chatUserId} Invalid Phone {messageText} Bot");

            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "invalidPhone"),
                ParseMode.Html,
                replyParameters: messageId);

            return;
        }

        string sessionPath = Path.Combine(AppContext.BaseDirectory, _appSettings.SessionsPath,
            string.Concat(phoneNumber, ".session"));

        InfoPhoneNumber infoPhoneNumber = Utils.InfoPhoneNumber(phoneNumber);

        bool anyExistsSessionDb = await sessionRepository.Any(infoPhoneNumber.CountryCode, infoPhoneNumber.PhoneNumber);
        bool anyExistsSessionFile = Utils.AnySessions(sessionPath);

        if (anyExistsSessionDb || anyExistsSessionFile)
        {
            _logger.Information($"- User {chatUserId} Exists Phone {phoneNumber} Bot");

            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "existsPhone"),
                ParseMode.Html,
                replyParameters: messageId);

            return;
        }


        Message msgWiteProsessing = await _telegramBotClient.SendMessage(chatUserId,
            Utils.GetText(eLanguageUser, "waite"), ParseMode.Html,
            replyParameters: messageId);

        Settings settings = await settingsRepository.GetSingleFirst();
        SessionInfo sessionInfo = await sessionInfoRepository.GetSingleFirst();

        Action<int, string> loging = (_, _) => { };

        if (settings.UseLogCLI)
        {
            loging = (_, msg) => { _logger.Information($"- Log Session : {phoneNumber}\n\n{msg}"); };
        }

        DeviceInfo deviceInfo = DeviceInfoManager.RandomDevice();

        IWTelegramManager wTelegramManager = wTelegramManagerBuilder.
                            WithApiId(sessionInfo.ApiId).
                            WithApiHash(sessionInfo.ApiHash).
                            WithDeviceModel(deviceInfo.DeviceModel).
                            WithSystemVersion(deviceInfo.SystemVersion).
                            WithAppVersion(deviceInfo.AppVersion).
                            WithLangCode(deviceInfo.LangCode).
                            WithSessionPath(sessionPath).
                            WithLoging(loging).
                            Build();

        try
        {
            await wTelegramManager.Connect();

            if (settings.UseProxy)
            {
                bool isConnected = false;

                int countProxy = ProxyManager.GetCount();

                for (int i = 0; i < countProxy; i++)
                {
                    Proxy proxy = ProxyManager.RandomProxy();

                    int port = int.Parse(proxy.Port);

                    isConnected = ProxyManager.IsConnectSocks5Proxy(proxy.Ip, port, proxy.Username, proxy.Password);

                    if (isConnected)
                    {
                        _logger.Information($"- Log Session: \n{phoneNumber}\n\nUse Proxy: {proxy.Ip}:{proxy.Port}");

                        wTelegramManager.UseScokcs5Proxy(proxy.Ip, port, proxy.Username, proxy.Password);

                        break;
                    }
                }

                if (!isConnected)
                {
                    _logger.Warning($"- Log Session : \n{phoneNumber}\n\nCant Connected Proxy");
                }
            }

            string state = await wTelegramManager.Login(phoneNumber);

            if (state == "email")
            {
                _logger.Information($"- User {chatUserId} Login Session {phoneNumber} State: {state} Bot");

                await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                    Utils.GetText(eLanguageUser, "numberIsNotEmailLogin"));

                await wTelegramManager.Disconnect();

                File.Delete(sessionPath);

                return;
            }

            if (string.IsNullOrEmpty(state) || state != "verification_code")
            {
                _logger.Information($"- User {chatUserId} Login Session {phoneNumber} State: {state} Bot");

                await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                    Utils.GetText(eLanguageUser, "tryAgain"));

                await wTelegramManager.Disconnect();

                File.Delete(sessionPath);

                return;
            }

            _logger.Information($"- User {chatUserId} Login Session {phoneNumber} State: {state} Bot");

            bool anyStep = await userStepRepository.Any(chatUserId);

            if (anyStep)
            {
                await userStepRepository.Remove(chatUserId);
            }

            await userStepRepository.Create(new()
            {
                ChatId = chatUserId,
                Step = "LoginCode",
                ExpierDateTime = Utils.GetDateTime(_appSettings.AskTimeOutMinute)
            });

            SessionCashManager.AddOrUpdate(chatUserId, new()
            {
                PhoneNumber = phoneNumber,
                SessionPath = sessionPath,
                InfoPhoneNumber = infoPhoneNumber,
                WTelegramManager = wTelegramManager,
                DeviceInfo = deviceInfo
            });

            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                string.Format(Utils.GetText(eLanguageUser, "loginCode"), phoneNumber));
        }
        catch (Exception ex) when (ex.Message == "PHONE_NUMBER_INVALID")
        {
            _logger.Information($"- User {chatUserId} Session {phoneNumber} Invalid Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "phoneInvalid"));
            await wTelegramManager.Disconnect();
            File.Delete(sessionPath);
        }
        catch (Exception ex) when (ex.Message == "PHONE_NUMBER_FLOOD")
        {
            _logger.Information($"- User {chatUserId} Session {phoneNumber} Flood Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "phoneFlood"));
            await wTelegramManager.Disconnect();
            File.Delete(sessionPath);
        }
        catch (Exception ex) when (ex.Message == "PHONE_NUMBER_BANNED")
        {
            _logger.Information($"- User {chatUserId} Session {phoneNumber} Ban Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "phoneBan"));
            await wTelegramManager.Disconnect();
            File.Delete(sessionPath);
        }
        catch (Exception ex) when (
            ex.Message.Contains("BadMsgNotification") ||
            ex.Message.Contains("Connection shut down") ||
            ex.Message.Contains("TimedOut") ||
            ex.Message.Contains("A connection attempt failed"))
        {
            _logger.Warning(ex, $"- User {chatUserId} Session {phoneNumber} Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "tryAgain"));
            await wTelegramManager.Disconnect();
            File.Delete(sessionPath);
        }
    }

    private async Task OnUpdateLoginCode(long chatUserId, int messageId, string messageText)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        IUserStepRepository userStepRepository = scope.ServiceProvider.GetRequiredService<IUserStepRepository>();
        ISessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        ISettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        ISessionInfoRepository sessionInfoRepository =
            scope.ServiceProvider.GetRequiredService<ISessionInfoRepository>();

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        bool anyCashe = SessionCashManager.Any(chatUserId);

        if (!anyCashe)
        {
            await userStepRepository.Remove(chatUserId);

            _logger.Information($"- User {chatUserId} Session Cashe Not Found Bot");

            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "tryAgain"), ParseMode.Html,
                replyParameters: messageId);

            return;
        }

        SessionCash sessionCash = SessionCashManager.Get(chatUserId);

        bool isValidLoginCode = int.TryParse(messageText, out _);
        const int validLenghtLoginCode = 5;

        if (!isValidLoginCode || messageText.Length != validLenghtLoginCode)
        {
            _logger.Information(
                $"- User {chatUserId} Invalid Login Code {messageText} For Phone {sessionCash.PhoneNumber} Bot");

            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "invalidCode"),
                ParseMode.Html,
                replyParameters: messageId);

            return;
        }

        await userStepRepository.Remove(chatUserId);
        SessionCashManager.Remove(chatUserId);

        Message msgWiteProsessing = await _telegramBotClient.SendMessage(chatUserId,
            Utils.GetText(eLanguageUser, "waite"), ParseMode.Html,
            replyParameters: messageId);

        IWTelegramManager wTelegramClientManager = sessionCash.WTelegramManager;

        try
        {
            string state = await wTelegramClientManager.Login(messageText);

            if (string.IsNullOrEmpty(state))
            {
                _logger.Information(
                    $"- User {chatUserId} Login Code {messageText} Correct Sucsessfully Login To {sessionCash.PhoneNumber} Bot");

                wTelegramClientManager.DisableUpdate();

                await wTelegramClientManager.ChangeStatus(online: true);

                Settings settings = await settingsRepository.GetSingleFirst();

                if (settings.UseCheckReport)
                {
                    bool isReport = await wTelegramClientManager.IsReport();

                    _logger.Information(
                        $"- User {chatUserId} State Report {sessionCash.PhoneNumber} Is {isReport} Bot");

                    if (isReport)
                    {
                        await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                            Utils.GetText(eLanguageUser, "phoneReport"));

                        await wTelegramClientManager.Logout();
                        await wTelegramClientManager.Disconnect();
                        File.Delete(sessionCash.SessionPath);

                        return;
                    }
                }

                if (settings.UseChangeBio)
                {
                    _logger.Information($"- User {chatUserId} Set Bio {sessionCash.PhoneNumber} Sucsessfully Bot");

                    await wTelegramClientManager.UpdateBio(_appSettings.Bio);
                }

                _logger.Information($"- User {chatUserId} Enable 2Fa Password Session {sessionCash.PhoneNumber} Bot");

                await wTelegramClientManager.EnablePassword2Fa(_appSettings.Password2Fa, _appSettings.Bio);
                await wTelegramClientManager.ChangeStatus(online: false);

                SessionInfo sessionInfo = await sessionInfoRepository.GetSingleFirst();
                User user = await userRepository.Get(chatUserId);

                Session session = new()
                {
                    CountryCode = sessionCash.InfoPhoneNumber.CountryCode,
                    Number = sessionCash.InfoPhoneNumber.PhoneNumber,
                    ESessionStatus = ESessionStatus.Exists,
                    RegisterDate = Utils.GetDateTime(),
                    SessionInfo = sessionInfo,
                    User = user,
                    DeviceAuthInfo = new()
                    {
                        DeviceModel = sessionCash.DeviceInfo.DeviceModel,
                        AppVersion = sessionCash.DeviceInfo.AppVersion,
                        LangCode = sessionCash.DeviceInfo.LangCode,
                        SystemVersion = sessionCash.DeviceInfo.SystemVersion
                    }
                };

                await sessionRepository.Create(session);

                await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                    string.Format(Utils.GetText(eLanguageUser, "loginSucsess"), sessionCash.PhoneNumber)
                    , replyMarkup: ReplyKeyboard.DownloadCurrentSession(Utils.GetText(eLanguageUser, "downloadCurrentSession"), sessionCash.PhoneNumber));

                await wTelegramClientManager.Disconnect();
            }
            else if (state == "verification_code")
            {
                _logger.Information(
                    $"- User {chatUserId} Invalid Login Code {messageText} Invalid {sessionCash.PhoneNumber} Bot");

                await userStepRepository.Create(new()
                {
                    ChatId = chatUserId,
                    Step = "LoginCode",
                    ExpierDateTime = Utils.GetDateTime(_appSettings.AskTimeOutMinute)
                });

                SessionCashManager.AddOrUpdate(chatUserId, sessionCash);

                await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                    string.Format(Utils.GetText(eLanguageUser, "loginCode"), sessionCash.PhoneNumber));
            }
            else if (state == "password")
            {
                _logger.Information($"- User {chatUserId} Need 2Fa Password For Phone {sessionCash.PhoneNumber} Bot");

                await userStepRepository.Create(new()
                {
                    ChatId = chatUserId,
                    Step = "Password2Fa",
                    ExpierDateTime = Utils.GetDateTime(_appSettings.AskTimeOutMinute)
                });

                SessionCashManager.AddOrUpdate(chatUserId, sessionCash);

                await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                    Utils.GetText(eLanguageUser, "password2Fa"));
            }
        }
        catch (Exception ex) when (
            ex.Message == "FROZEN_METHOD_INVALID" ||
            ex.Message.Contains("FROZEN"))
        {
            _logger.Information($"- User {chatUserId} Session {sessionCash.PhoneNumber} Frozen Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "phoneFrozen"));
            await wTelegramClientManager.Logout();
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionCash.SessionPath);
        }
        catch (Exception ex) when (
            ex.Message == "AUTH_KEY_UNREGISTERED" ||
            ex.Message == "SESSION_REVOKED")
        {
            _logger.Information($"- User {chatUserId} Session {sessionCash.PhoneNumber} Takeout / Revoked Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "phoneRevoked"));
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionCash.SessionPath);
        }
        catch (Exception ex) when (ex.Message == "PHONE_CODE_EXPIRED")
        {
            _logger.Information($"- User {chatUserId} Session {sessionCash.PhoneNumber} Login Code Expire Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "loginCodeExpire"));
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionCash.SessionPath);
        }
        catch (Exception ex) when (
            ex.Message == "USER_DEACTIVATED" ||
            ex.Message == "USER_DEACTIVATED_BAN" ||
            ex.Message == "PHONE_NUMBER_BANNED")
        {
            _logger.Information($"- User {chatUserId} Session {sessionCash.PhoneNumber} Ban Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "phoneBan"));
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionCash.PhoneNumber);
        }
        catch (Exception ex) when (
            ex.Message.Contains("BadMsgNotification") ||
            ex.Message.Contains("Connection shut down") ||
            ex.Message.Contains("TimedOut") ||
            ex.Message.Contains("A connection attempt failed"))
        {
            _logger.Warning(ex, $"- User {chatUserId} Login Session {sessionCash.PhoneNumber} Bot");
            await userStepRepository.Create(new()
            {
                ChatId = chatUserId,
                Step = "LoginCode",
                ExpierDateTime = Utils.GetDateTime(_appSettings.AskTimeOutMinute)
            });

            SessionCashManager.AddOrUpdate(chatUserId, sessionCash);

            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                string.Format(Utils.GetText(eLanguageUser, "loginCode"), sessionCash.PhoneNumber));
        }
    }

    private async Task OnUpdatePassword2Fa(long chatUserId, int messageId, string messageText)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        IUserStepRepository userStepRepository = scope.ServiceProvider.GetRequiredService<IUserStepRepository>();
        ISessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        ISettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        ISessionInfoRepository sessionInfoRepository =
            scope.ServiceProvider.GetRequiredService<ISessionInfoRepository>();

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        bool anyCashe = SessionCashManager.Any(chatUserId);

        if (!anyCashe)
        {
            await userStepRepository.Remove(chatUserId);

            _logger.Information($"- User {chatUserId} Session Cashe Not Found Bot");

            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "tryAgain"), ParseMode.Html,
                replyParameters: messageId);

            return;
        }

        SessionCash sessionCash = SessionCashManager.Get(chatUserId);

        await userStepRepository.Remove(chatUserId);
        SessionCashManager.Remove(chatUserId);

        Message msgWiteProsessing = await _telegramBotClient.SendMessage(chatUserId,
            Utils.GetText(eLanguageUser, "waite"), ParseMode.Html,
            replyParameters: messageId);

        IWTelegramManager wTelegramClientManager = sessionCash.WTelegramManager;

        try
        {
            string state = await wTelegramClientManager.Login(messageText);

            if (string.IsNullOrEmpty(state))
            {
                _logger.Information(
                    $"- User {chatUserId} Login Password2Fa {messageText} Correct Sucsessfully Login To {sessionCash.PhoneNumber} Bot");

                wTelegramClientManager.DisableUpdate();

                await wTelegramClientManager.ChangeStatus(online: true);

                await wTelegramClientManager.DisablePassword2Fa(messageText);

                _logger.Information(
                    $"- User {chatUserId} Disable Password2Fa {messageText} Phone {sessionCash.PhoneNumber} Bot");

                Settings settings = await settingsRepository.GetSingleFirst();

                if (settings.UseCheckReport)
                {
                    bool isReport = await wTelegramClientManager.IsReport();

                    _logger.Information(
                        $"- User {chatUserId} State Report {sessionCash.PhoneNumber} Is {isReport} Bot");

                    if (isReport)
                    {
                        await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                            Utils.GetText(eLanguageUser, "phoneReport"));

                        await wTelegramClientManager.Logout();
                        await wTelegramClientManager.Disconnect();
                        File.Delete(sessionCash.SessionPath);

                        return;
                    }
                }

                if (settings.UseChangeBio)
                {
                    _logger.Information($"- User {chatUserId} Set Bio {sessionCash.PhoneNumber} Sucsessfully Bot");

                    await wTelegramClientManager.UpdateBio(_appSettings.Bio);
                }

                _logger.Information($"- User {chatUserId} Enable 2Fa Password Session {sessionCash.PhoneNumber} Bot");

                await wTelegramClientManager.EnablePassword2Fa(_appSettings.Password2Fa, _appSettings.Bio);

                await wTelegramClientManager.ChangeStatus(online: false);

                SessionInfo sessionInfo = await sessionInfoRepository.GetSingleFirst();
                User user = await userRepository.Get(chatUserId);

                await sessionRepository.Create(new()
                {
                    CountryCode = sessionCash.InfoPhoneNumber.CountryCode,
                    Number = sessionCash.InfoPhoneNumber.PhoneNumber,
                    ESessionStatus = ESessionStatus.Exists,
                    RegisterDate = Utils.GetDateTime(),
                    SessionInfo = sessionInfo,
                    User = user,
                    DeviceAuthInfo = new()
                    {
                        DeviceModel = sessionCash.DeviceInfo.DeviceModel,
                        AppVersion = sessionCash.DeviceInfo.AppVersion,
                        LangCode = sessionCash.DeviceInfo.LangCode,
                        SystemVersion = sessionCash.DeviceInfo.SystemVersion
                    }
                });

                await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                    string.Format(Utils.GetText(eLanguageUser, "loginSucsess"), sessionCash.PhoneNumber)
                    , replyMarkup: ReplyKeyboard.DownloadCurrentSession(Utils.GetText(eLanguageUser, "downloadCurrentSession"), sessionCash.PhoneNumber));

                await wTelegramClientManager.Disconnect();
            }
            else if (state == "password")
            {
                _logger.Information($"- User {chatUserId} Need 2Fa Password For Phone {sessionCash.PhoneNumber} Bot");

                await userStepRepository.Create(new()
                {
                    ChatId = chatUserId,
                    Step = "Password2Fa",
                    ExpierDateTime = Utils.GetDateTime(_appSettings.AskTimeOutMinute)
                });

                SessionCashManager.AddOrUpdate(chatUserId, sessionCash);

                await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                    Utils.GetText(eLanguageUser, "password2Fa"));
            }
        }
        catch (Exception ex) when (
            ex.Message == "FROZEN_METHOD_INVALID" ||
            ex.Message.Contains("FROZEN"))
        {
            _logger.Information($"- User {chatUserId} Session {sessionCash.PhoneNumber} Frozen Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "phoneFrozen"));
            await wTelegramClientManager.Logout();
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionCash.SessionPath);
        }
        catch (Exception ex) when (ex.Message == "PHONE_PASSWORD_FLOOD")
        {
            _logger.Information($"- User {chatUserId} Session {sessionCash.PhoneNumber} Flood 2Fa Password Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "password2FaFlood"));
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionCash.SessionPath);
        }
        catch (Exception ex) when (
            ex.Message == "AUTH_KEY_UNREGISTERED" ||
            ex.Message == "SESSION_REVOKED")
        {
            _logger.Information($"- User {chatUserId} Session {sessionCash.PhoneNumber} Takeout / Revoked Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "phoneRevoked"));
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionCash.SessionPath);
        }
        catch (Exception ex) when (
            ex.Message == "USER_DEACTIVATED" ||
            ex.Message == "USER_DEACTIVATED_BAN" ||
            ex.Message == "PHONE_NUMBER_BANNED")
        {
            _logger.Information($"- User {chatUserId} Session {sessionCash.PhoneNumber} Ban Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "phoneBan"));
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionCash.PhoneNumber);
        }
        catch (Exception ex) when (
            ex.Message.Contains("BadMsgNotification") ||
            ex.Message.Contains("Connection shut down") ||
            ex.Message.Contains("TimedOut") ||
            ex.Message.Contains("A connection attempt failed"))
        {
            _logger.Warning(ex, $"- User {chatUserId} Login Session {sessionCash.PhoneNumber} Bot");

            await userStepRepository.Create(new()
            {
                ChatId = chatUserId,
                Step = "Password2Fa",
                ExpierDateTime = Utils.GetDateTime(_appSettings.AskTimeOutMinute)
            });

            SessionCashManager.AddOrUpdate(chatUserId, sessionCash);

            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId,
                Utils.GetText(eLanguageUser, "password2Fa"));
        }
    }

    private async Task OnHelp(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (anySudo)
        {
            return;
        }

        _logger.Information($"- User {chatUserId} /help bot");

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "help"), ParseMode.Html,
            replyParameters: messageId);
    }

    private async Task OnUpdateBackupSessions(long chatUserId, int messageId, string callbackData)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        ISessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        callbackData = callbackData.Replace("BackupSessions_", string.Empty);
        long userChatId = Convert.ToInt64(callbackData);

        _logger.Information($"- Sudo {chatUserId} Click Backup Sessions User {userChatId}");

        Message msgRemoveBtn =
            await _telegramBotClient.EditMessageReplyMarkup(chatUserId, messageId, replyMarkup: null);

        IEnumerable<Session> sessions = await sessionRepository.GetAll(userChatId);

        int sessionCount = sessions.Count();

        ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

        if (sessionCount == default)
        {
            _logger.Information($"- Sudo {chatUserId} User {userChatId} Not Available Session");
            await _telegramBotClient.SendMessage(chatUserId, string.Format(
                    Utils.GetText(eLanguageSudo, "sessionNotAvailable"),
                    userChatId),
                replyParameters: messageId);

            await _telegramBotClient.EditMessageReplyMarkup(chatUserId, msgRemoveBtn.MessageId,
                replyMarkup: ReplyKeyboard.StatusPermisionUser(userChatId,
                    Utils.GetText(eLanguageSudo, "changePeremission"),
                    Utils.GetText(eLanguageSudo, "backupUserSessions")));
            return;
        }

        string randomFileName = Path.GetRandomFileName();

        string fileName = $"Backup Sessions User {userChatId} {randomFileName}.zip";

        ZipArchive zipArchive = ZipFile.Open(fileName, ZipArchiveMode.Create);

        foreach (var session in sessions)
        {
            string phoneNumber = $"{session.CountryCode}{session.Number}";

            string sessionPath = Path.Combine(AppContext.BaseDirectory, _appSettings.SessionsPath,
                string.Concat(phoneNumber, ".session"));

            string jsonFileName = $"{phoneNumber}.json";

            string jsonPath = Path.Combine(AppContext.BaseDirectory, jsonFileName);

            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }

            await using FileStream fileStream = new(jsonFileName, FileMode.CreateNew);
            await using StreamWriter writerStream = new(fileStream, Encoding.UTF8);

            var sessionMapToJsonType = new
            {
                session.CountryCode,
                session.Number,
                session.DeviceAuthInfo.DeviceModel,
                session.DeviceAuthInfo.SystemVersion,
                session.DeviceAuthInfo.AppVersion,
                session.DeviceAuthInfo.LangCode
            };

            string dataSessionToJson =
                JsonSerializer.Serialize(sessionMapToJsonType, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });


            await writerStream.WriteAsync(dataSessionToJson);

            writerStream.Close();
            fileStream.Close();

            zipArchive.CreateEntryFromFile(sessionPath, Path.GetFileName(sessionPath));
            zipArchive.CreateEntryFromFile(jsonPath, jsonFileName);

            File.Delete(jsonPath);
        }

        zipArchive.Dispose();

        string zipPath = Path.Combine(AppContext.BaseDirectory, fileName);

        StreamReader streamReader = new(zipPath);

        await _telegramBotClient.SendChatAction(chatUserId, ChatAction.UploadDocument);

        await _telegramBotClient.SendDocument(chatUserId, streamReader.BaseStream,
            string.Format(Utils.GetText(eLanguageSudo, "backupSessionsWarning"), userChatId),
            replyParameters: messageId);

        streamReader.Close();

        File.Delete(zipPath);
    }

    private async Task OnUpdateDownloadCurrentSession(long chatUserId, int messageId, string callbackData)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        ISessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        string phoneNumber = callbackData.Replace("Download_", string.Empty);

        string sessionPath = Path.Combine(AppContext.BaseDirectory, _appSettings.SessionsPath,
            string.Concat(phoneNumber, ".session"));

        await _telegramBotClient.EditMessageReplyMarkup(chatUserId, messageId, replyMarkup: null);

        _logger.Information($"- User {chatUserId} Click Download Current Session {phoneNumber} Bot");

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        InfoPhoneNumber infoPhoneNumber = Utils.InfoPhoneNumber(phoneNumber);

        bool anyExistsSessionDb = await sessionRepository.Any(infoPhoneNumber.CountryCode, infoPhoneNumber.PhoneNumber);
        bool anyExistsSessionFile = Utils.AnySessions(sessionPath);

        if (!anyExistsSessionDb || !anyExistsSessionFile)
        {
            _logger.Information($"- Session File Not Exist {phoneNumber} Bot");

            await _telegramBotClient.EditMessageText(chatUserId, messageId,
                string.Format(Utils.GetText(eLanguageUser, "sessionFileNotExist"), phoneNumber));

            return;
        }

        Session session = await sessionRepository.Get(infoPhoneNumber.CountryCode, infoPhoneNumber.PhoneNumber);

        if (session.ESessionStatus is ESessionStatus.Sold)
        {
            _logger.Information($"- Session File Status {phoneNumber} Sold Bot");

            await _telegramBotClient.EditMessageText(chatUserId, messageId,
                string.Format(Utils.GetText(eLanguageUser, "sessionFileNotExist"), phoneNumber));

            return;
        }

        await sessionRepository.UpdateStatus(session, ESessionStatus.Sold);

        string randomFileName = Path.GetRandomFileName();

        string zipFileName = $"{phoneNumber} {randomFileName}.zip";
        string jsonFileName = $"{phoneNumber}.json";

        string jsonPath = Path.Combine(AppContext.BaseDirectory, jsonFileName);

        if (File.Exists(jsonPath))
        {
            File.Delete(jsonPath);
        }

        await using FileStream fileStream = new(jsonFileName, FileMode.CreateNew);
        await using StreamWriter writerStream = new(fileStream, Encoding.UTF8);

        var sessionMapToJsonType = new
        {
            session.CountryCode,
            session.Number,
            session.DeviceAuthInfo.DeviceModel,
            session.DeviceAuthInfo.SystemVersion,
            session.DeviceAuthInfo.AppVersion,
            session.DeviceAuthInfo.LangCode
        };

        string dataSessionToJson =
            JsonSerializer.Serialize(sessionMapToJsonType, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });


        await writerStream.WriteAsync(dataSessionToJson);

        writerStream.Close();
        fileStream.Close();

        using ZipArchive zipArchive = ZipFile.Open(zipFileName, ZipArchiveMode.Create);
        zipArchive.CreateEntryFromFile(sessionPath, Path.GetFileName(sessionPath));
        zipArchive.CreateEntryFromFile(jsonPath, jsonFileName);

        zipArchive.Dispose();

        string zipPath = Path.Combine(AppContext.BaseDirectory, zipFileName);

        StreamReader streamReader = new(zipPath);

        await _telegramBotClient.SendChatAction(chatUserId, ChatAction.UploadDocument);

        await _telegramBotClient.SendDocument(chatUserId, streamReader.BaseStream,
            Utils.GetText(eLanguageUser, "descriptionUploadSession"), replyParameters: messageId);

        streamReader.Close();

        File.Delete(zipPath);
        File.Delete(jsonPath);
    }

    private async Task OnSettings(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        ISettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (!anySudo)
        {
            return;
        }

        Settings settings = await settingsRepository.GetSingleFirst();

        _logger.Information($"- Sudo {chatUserId} /settings Bot");

        ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

        await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageSudo, "settings"),
            ParseMode.Html,
            replyParameters: messageId,
            replyMarkup: ReplyKeyboard.Settings(settings,
                Utils.GetText(eLanguageSudo, "useProxy"),
                Utils.GetText(eLanguageSudo, "useChangeBio"),
                Utils.GetText(eLanguageSudo, "useCheckReport"),
                Utils.GetText(eLanguageSudo, "useLogCLi")));
    }

    private async Task OnUpdateUseCheckReport(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        ISettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (!anySudo)
        {
            return;
        }

        _logger.Information($"- Sudo {chatUserId} Click Update Use Check Report Bot");

        Message msgRemoveButton =
            await _telegramBotClient.EditMessageReplyMarkup(chatUserId, messageId, replyMarkup: null);

        ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

        Settings settings = await settingsRepository.GetSingleFirst();

        if (settings.UseCheckReport)
        {
            settings.UseCheckReport = false;
            _logger.Information($"- Sudo {chatUserId} Update Use Check ReportTrue To False Bot");
        }
        else
        {
            settings.UseCheckReport = true;
            _logger.Information($"- Sudo {chatUserId} Update Use Check Report True To False Bot");
        }

        await settingsRepository.Update(settings);

        await _telegramBotClient.EditMessageText(chatUserId, msgRemoveButton.MessageId,
            Utils.GetText(eLanguageSudo, "settings"),
            ParseMode.Html,
            replyMarkup: ReplyKeyboard.Settings(settings,
                Utils.GetText(eLanguageSudo, "useProxy"),
                Utils.GetText(eLanguageSudo, "useChangeBio"),
                Utils.GetText(eLanguageSudo, "useCheckReport"),
                Utils.GetText(eLanguageSudo, "useLogCLi")));
    }

    private async Task OnUpdateUseLogCLi(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        ISettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (!anySudo)
        {
            return;
        }

        _logger.Information($"- Sudo {chatUserId} Click Update UseLog CLi Bot");

        Message msgRemoveButtotn =
            await _telegramBotClient.EditMessageReplyMarkup(chatUserId, messageId, replyMarkup: null);

        ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

        Settings settings = await settingsRepository.GetSingleFirst();

        if (settings.UseLogCLI)
        {
            settings.UseLogCLI = false;
            _logger.Information($"- Sudo {chatUserId} Update UseLog CLi True To False Bot");
        }
        else
        {
            settings.UseLogCLI = true;
            _logger.Information($"- Sudo {chatUserId} Update UseLog CLi True To False Bot");
        }

        await settingsRepository.Update(settings);

        await _telegramBotClient.EditMessageText(chatUserId, msgRemoveButtotn.MessageId,
            Utils.GetText(eLanguageSudo, "settings"),
            ParseMode.Html,
            replyMarkup: ReplyKeyboard.Settings(settings,
                Utils.GetText(eLanguageSudo, "useProxy"),
                Utils.GetText(eLanguageSudo, "useChangeBio"),
                Utils.GetText(eLanguageSudo, "useCheckReport"),
                Utils.GetText(eLanguageSudo, "useLogCLi")));
    }

    private async Task OnUpdateUseChangeBio(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        ISettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (!anySudo)
        {
            return;
        }

        _logger.Information($"- Sudo {chatUserId} Click Update Change Bio Bot");

        Message msgRemoveButtotn =
            await _telegramBotClient.EditMessageReplyMarkup(chatUserId, messageId, replyMarkup: null);

        ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

        Settings settings = await settingsRepository.GetSingleFirst();

        if (settings.UseChangeBio)
        {
            settings.UseChangeBio = false;
            _logger.Information($"- Sudo {chatUserId} Update Change Bio True To False Bot");
        }
        else
        {
            settings.UseChangeBio = true;
            _logger.Information($"- Sudo {chatUserId} Update Change Bio True To False Bot");
        }

        await settingsRepository.Update(settings);

        await _telegramBotClient.EditMessageText(chatUserId, msgRemoveButtotn.MessageId,
            Utils.GetText(eLanguageSudo, "settings"),
            ParseMode.Html,
            replyMarkup: ReplyKeyboard.Settings(settings,
                Utils.GetText(eLanguageSudo, "useProxy"),
                Utils.GetText(eLanguageSudo, "useChangeBio"),
                Utils.GetText(eLanguageSudo, "useCheckReport"),
                Utils.GetText(eLanguageSudo, "useLogCLi")));
    }

    private async Task OnUpdateUseProxy(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        ISettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (!anySudo)
        {
            return;
        }

        _logger.Information($"- Sudo {chatUserId} Click Update Use Proxy Bot");

        Message msgRemoveButtotn =
            await _telegramBotClient.EditMessageReplyMarkup(chatUserId, messageId, replyMarkup: null);

        ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

        Settings settings = await settingsRepository.GetSingleFirst();

        if (!File.Exists(_appSettings.ProxyPath) || ProxyManager.HaveData(_appSettings.ProxyPath) is false)
        {
            _logger.Information($"- Sudo {chatUserId} Data or file Proxy Not Exists");

            await _telegramBotClient.EditMessageText(chatUserId, msgRemoveButtotn.MessageId,
                Utils.GetText(eLanguageSudo, "notExistData"), parseMode: ParseMode.Html,
                replyMarkup: ReplyKeyboard.Settings(settings,
                    Utils.GetText(eLanguageSudo, "useProxy"),
                    Utils.GetText(eLanguageSudo, "useChangeBio"),
                    Utils.GetText(eLanguageSudo, "useCheckReport"),
                    Utils.GetText(eLanguageSudo, "useLogCLi")));

            return;
        }

        if (settings.UseProxy)
        {
            ProxyManager.CleanCashe();
            settings.UseProxy = false;

            _logger.Information($"- Sudo {chatUserId} Update Use Proxy True To False Bot");
        }
        else
        {
            ProxyManager.SetProxy(_appSettings.ProxyPath);
            settings.UseProxy = true;

            _logger.Information($"- Sudo {chatUserId} Update Use Proxy False To True Bot");
        }

        await settingsRepository.Update(settings);

        await _telegramBotClient.EditMessageText(chatUserId, msgRemoveButtotn.MessageId,
            Utils.GetText(eLanguageSudo, "settings"),
            ParseMode.Html,
            replyMarkup: ReplyKeyboard.Settings(settings,
                Utils.GetText(eLanguageSudo, "useProxy"),
                Utils.GetText(eLanguageSudo, "useChangeBio"),
                Utils.GetText(eLanguageSudo, "useCheckReport"),
                Utils.GetText(eLanguageSudo, "useLogCLi")));
    }

    private async Task OnStep(long chatUserId, int messageId, string messageText)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        IUserStepRepository userStepRepository = scope.ServiceProvider.GetRequiredService<IUserStepRepository>();
        ISessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        bool anyStep = await userStepRepository.Any(chatUserId);

        if (!anyStep)
        {
            await OnUpdatePhone(chatUserId, messageId, messageText);

            return;
        }

        UserStep userStep = await userStepRepository.Get(chatUserId);

        string step = userStep.Step;

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (anySudo)
        {
            ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

            if (step == "GetUserChatId")
            {
                bool validInput = long.TryParse(messageText, out long userChatId);

                if (!validInput)
                {
                    await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageSudo, "invalidInput"),
                        ParseMode.Html);
                    return;
                }

                bool anyUser = await userRepository.Any(userChatId);

                if (!anyUser)
                {
                    await _telegramBotClient.SendMessage(chatUserId,
                        string.Format(Utils.GetText(eLanguageSudo, "userNotExist"), userChatId),
                        ParseMode.Html);
                    return;
                }

                await userStepRepository.Remove(chatUserId);

                IEnumerable<Session> sessions = await sessionRepository.GetAll(userChatId);

                // ReSharper disable once PossibleMultipleEnumeration
                int countSessionExists = sessions.Count(x => x.ESessionStatus is ESessionStatus.Exists);
                // ReSharper disable once PossibleMultipleEnumeration
                int countSessionSold = sessions.Count(x => x.ESessionStatus is ESessionStatus.Sold);

                bool isPermissionToUse = await userRepository.IsPermisionToUse(userChatId);

                string resultPermissionToUse = isPermissionToUse ? "🟢" : "🔴";

                await _telegramBotClient.SendMessage(chatUserId,
                    string.Format(Utils.GetText(eLanguageSudo, "infoUser"), userChatId, countSessionExists,
                        countSessionSold, resultPermissionToUse),
                    ParseMode.Html,
                    replyParameters: messageId,
                    replyMarkup: ReplyKeyboard.StatusPermisionUser(userChatId,
                        Utils.GetText(eLanguageSudo, "changePeremission"),
                        Utils.GetText(eLanguageSudo, "backupUserSessions")));
            }

            return;
        }

        switch (userStep.Step)
        {
            case "LoginCode":
                {
                    await OnUpdateLoginCode(chatUserId, messageId, messageText);
                }
                break;
            case "Password2Fa":
                {
                    await OnUpdatePassword2Fa(chatUserId, messageId, messageText);
                }
                break;
        }
    }

    private async Task OnReload(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (!anySudo)
        {
            return;
        }

        _logger.Information($"- Sudo {chatUserId} /reload Bot");

        ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

        await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageSudo, "reloadAlert"),
            ParseMode.Html,
            replyParameters: messageId,
            replyMarkup: ReplyKeyboard.Reload(Utils.GetText(eLanguageSudo, "reloadConfirm")));
    }

    private async Task OnCancel(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        IUserStepRepository userStepRepository = scope.ServiceProvider.GetRequiredService<IUserStepRepository>();

        bool anyStep = await userStepRepository.Any(chatUserId);

        if (anyStep)
        {
            await userStepRepository.Remove(chatUserId);
        }

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (anySudo)
        {
            _logger.Information($"- Sudo {chatUserId} /cancel bot");

            ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

            await _telegramBotClient.SendMessage(chatUserId,
                Utils.GetText(eLanguageSudo, "cancelTask"), ParseMode.Html,
                replyParameters: messageId);

            return;
        }

        bool anySessionCashe = SessionCashManager.Any(chatUserId);

        if (anySessionCashe)
        {
            SessionCash sessionCash = SessionCashManager.Get(chatUserId);

            IWTelegramManager wTelegramClientManager = sessionCash.WTelegramManager;

            await wTelegramClientManager.Disconnect();

            File.Delete(sessionCash.SessionPath);
        }

        _logger.Information($"- User {chatUserId} /cancel bot");

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        await _telegramBotClient.SendMessage(chatUserId,
            Utils.GetText(eLanguageUser, "cancelTask"), ParseMode.Html,
            replyParameters: messageId);
    }

    private async Task OnInfoUser(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        ISessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        IUserStepRepository userStepRepository = scope.ServiceProvider.GetRequiredService<IUserStepRepository>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (anySudo)
        {
            _logger.Information($"- Sudo {chatUserId} /info bot");

            bool anyStep = await userStepRepository.Any(chatUserId);
             
            if (anyStep)
            {
                await userStepRepository.Remove(chatUserId);
            }

            await userStepRepository.Create(new()
            {
                Step = "GetUserChatId",
                ChatId = chatUserId,
                ExpierDateTime = Utils.GetDateTime(_appSettings.AskTimeOutMinute)
            });

            ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

            await _telegramBotClient.SendMessage(chatUserId,
                Utils.GetText(eLanguageSudo, "askUserChatId"), ParseMode.Html,
                replyParameters: messageId);

            return;
        }

        _logger.Information($"- User {chatUserId} /info bot");

        IEnumerable<Session> sessions = await sessionRepository.GetAll(chatUserId);

        // ReSharper disable once PossibleMultipleEnumeration
        int countSessionExists = sessions.Count(x => x.ESessionStatus is ESessionStatus.Exists);
        // ReSharper disable once PossibleMultipleEnumeration
        int countSessionSold = sessions.Count(x => x.ESessionStatus is ESessionStatus.Sold);

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        bool isPermissionToUse = await userRepository.IsPermisionToUse(chatUserId);

        string resultPermissionToUse = isPermissionToUse ? "🟢" : "🔴";

        await _telegramBotClient.SendMessage(chatUserId,
            string.Format(Utils.GetText(eLanguageUser, "infoUser"), chatUserId, countSessionExists, countSessionSold,
                resultPermissionToUse),
            ParseMode.Html,
            replyParameters: messageId);
    }

    private async Task OnLanguage(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (anySudo)
        {
            _logger.Information($"- Sudo {chatUserId} /language bot");

            Sudo sudo = await sudoRepository.Get(chatUserId);

            switch (sudo.Language)
            {
                case ELanguage.En:
                    {
                        await sudoRepository.ChangeLanguage(sudo, ELanguage.Fa);
                    }
                    break;
                case ELanguage.Fa:
                    {
                        await sudoRepository.ChangeLanguage(sudo, ELanguage.En);
                    }
                    break;
                default:
                    {
                        await sudoRepository.ChangeLanguage(sudo, ELanguage.En);
                    }
                    break;
            }

            ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageSudo, "languageChanged"),
                ParseMode.Html, replyParameters: messageId);

            return;
        }

        _logger.Information($"- User {chatUserId} /language bot");

        User user = await userRepository.Get(chatUserId);

        switch (user.Language)
        {
            case ELanguage.En:
                {
                    await userRepository.ChangeLanguage(user, ELanguage.Fa);
                }
                break;
            case ELanguage.Fa:
                {
                    await userRepository.ChangeLanguage(user, ELanguage.En);
                }
                break;
            default:
                {
                    await userRepository.ChangeLanguage(user, ELanguage.En);
                }
                break;
        }

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "languageChanged"),
            ParseMode.Html, replyParameters: messageId);
    }

    private async Task OnStart(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        bool anySudo = await sudoRepository.Any(chatUserId);

        if (anySudo)
        {
            _logger.Information($"- Sudo {chatUserId} /start bot");

            ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageSudo, "start"), ParseMode.Html,
                replyParameters: messageId);

            if (_appSettings.CommandsSudo.Count == default)
            {
                return;
            }

            ICollection<BotCommand> sudoCommands = [];

            foreach (var item in _appSettings.CommandsSudo)
            {
                sudoCommands.Add(new()
                {
                    Command = item.Key,
                    Description = item.Value
                });
            }

            _logger.Information($"- Sudo {chatUserId} command updated");

            await _telegramBotClient.SetMyCommands(sudoCommands.Reverse(),
                new BotCommandScopeChat() { ChatId = chatUserId });

            return;
        }

        bool anyUser = await userRepository.Any(chatUserId);

        if (!anyUser)
        {
            await userRepository.Create(new()
            {
                ChatId = chatUserId,
                Language = ELanguage.En
            });

            _logger.Information($"- User {chatUserId} signup to bot");
        }

        _logger.Information($"- User {chatUserId} /start bot");

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        Dictionary<string, string> usernameJoinForces = await IsJoinForce(chatUserId);

        if (usernameJoinForces.Count > default(int))
        {
            _logger.Information($"- User {chatUserId} show list to force join bot");
            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "joinRequest"),
                ParseMode.Html, replyParameters: messageId,
                replyMarkup: ReplyKeyboard.JoinForce(usernameJoinForces, Utils.GetText(eLanguageUser, "joined")));
            return;
        }

        bool isPermissionToUse = await IsPermissionToUse(chatUserId);

        if (!isPermissionToUse)
        {
            _logger.Information($"- User {chatUserId} access denied use a bot");
            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "acsessDenidUseBot"),
                ParseMode.Html, replyParameters: messageId,
                replyMarkup: ReplyKeyboard.Developer(Utils.GetText(eLanguageUser, "developer"),
                    _appSettings.Developer));
            return;
        }

        await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "start"), ParseMode.Html,
            replyParameters: messageId);

        if (_appSettings.CommandsUser.Count == default)
        {
            return;
        }

        ICollection<BotCommand> userCommands = [];

        foreach (var item in _appSettings.CommandsUser)
        {
            userCommands.Add(new()
            {
                Command = item.Key,
                Description = item.Value
            });
        }

        _logger.Information($"- User {chatUserId} command updated");

        await _telegramBotClient.SetMyCommands(userCommands.Reverse(),
            new BotCommandScopeChat() { ChatId = chatUserId });
    }

    private async Task OnUpdateReload(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserStepRepository userStepRepository = _serviceProvider.GetRequiredService<IUserStepRepository>();

        _logger.Information($"- Sudo {chatUserId} Confirm Reload Bot");

        Message removeButton =
            await _telegramBotClient.EditMessageReplyMarkup(chatUserId, messageId, replyMarkup: null);

        bool anyStep = await userStepRepository.Any(chatUserId);

        if (anyStep)
        {
            await userStepRepository.Remove(chatUserId);
        }

        Process infoCurrentProcess = Process.GetCurrentProcess();

        string fileName = infoCurrentProcess.MainModule?.FileName!;

        string mutexName = Path.GetFileNameWithoutExtension(fileName);

        using Mutex mutexReload = new(true, mutexName, out bool createdNew);

        ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

        if (!createdNew)
        {
            await _telegramBotClient.EditMessageText(chatUserId, removeButton.MessageId,
                Utils.GetText(eLanguageSudo, "reloadMultipale"), replyMarkup: null);
            return;
        }

        _logger.Information($"- Sudo {chatUserId} Reload Bot Sucsessfully");

        await _telegramBotClient.EditMessageText(chatUserId, removeButton.MessageId,
            Utils.GetText(eLanguageSudo, "reloadSucsess"), replyMarkup: null);

        try
        {
            Process.Start(fileName);
            Environment.Exit(0);
        }
        finally
        {
            mutexReload.ReleaseMutex();
        }
    }

    private async Task OnUpdateChangePermission(long chatUserId, int messageId, string callbackData)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        ISessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        callbackData = callbackData.Replace("ChangePermission_", string.Empty);
        long userChatId = Convert.ToInt64(callbackData);

        _logger.Information($"- Sudo {chatUserId} Click Change Permission User {userChatId}");

        User user = await userRepository.Get(userChatId);

        if (user.IsPermissionToUse)
        {
            _logger.Information($"- Sudo {chatUserId} Click Change Permission User {userChatId} To Unauthorize");
            await userRepository.UnauthorizedPermisionToUse(user);
        }
        else
        {
            _logger.Information($"- Sudo {chatUserId} Click Change Permission User {userChatId} To Authorize");
            await userRepository.AuthorizedPermisionToUse(user);
        }

        IEnumerable<Session> sessions = await sessionRepository.GetAll(userChatId);

        // ReSharper disable once PossibleMultipleEnumeration
        int countSessionExists = sessions.Count(x => x.ESessionStatus is ESessionStatus.Exists);
        // ReSharper disable once PossibleMultipleEnumeration
        int countSessionSold = sessions.Count(x => x.ESessionStatus is ESessionStatus.Sold);

        bool isPermissionToUse = await userRepository.IsPermisionToUse(userChatId);

        string resultPermissionToUse = isPermissionToUse ? "🟢" : "🔴";

        ELanguage eLanguageSudo = await sudoRepository.GetLanguage(chatUserId);

        await _telegramBotClient.EditMessageText(chatUserId, messageId,
            string.Format(Utils.GetText(eLanguageSudo, "infoUser"), userChatId, countSessionExists,
                countSessionSold, resultPermissionToUse),
            parseMode: ParseMode.Html,
            replyMarkup: ReplyKeyboard.StatusPermisionUser(userChatId,
                Utils.GetText(eLanguageSudo, "changePeremission"),
                Utils.GetText(eLanguageSudo, "backupUserSessions")));
    }

    private async Task OnUpdateIJoin(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        Message msgEditReply =
            await _telegramBotClient.EditMessageReplyMarkup(chatUserId, messageId, replyMarkup: null);

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        Dictionary<string, string> usernameJoinForces = await IsJoinForce(chatUserId);

        if (usernameJoinForces.Count > default(int))
        {
            _logger.Information($"- User {chatUserId} show list to force join bot");

            await _telegramBotClient.EditMessageReplyMarkup(chatUserId, msgEditReply.MessageId,
                replyMarkup: ReplyKeyboard.JoinForce(usernameJoinForces, Utils.GetText(eLanguageUser, "joined")));

            return;
        }

        bool isPermissionToUse = await IsPermissionToUse(chatUserId);

        if (!isPermissionToUse)
        {
            _logger.Information($"- User {chatUserId} access denied use a bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgEditReply.MessageId,
                Utils.GetText(eLanguageUser, "acsessDenidUseBot"),
                ParseMode.Html,
                replyMarkup: ReplyKeyboard.Developer(Utils.GetText(eLanguageUser, "developer"),
                    _appSettings.Developer));
            return;
        }

        await _telegramBotClient.EditMessageText(chatUserId, msgEditReply.MessageId,
            Utils.GetText(eLanguageUser, "start"), ParseMode.Html);

        if (_appSettings.CommandsUser.Count == default)
        {
            return;
        }

        ICollection<BotCommand> userCommands = [];

        foreach (var item in _appSettings.CommandsUser)
        {
            userCommands.Add(new()
            {
                Command = item.Key,
                Description = item.Value
            });
        }

        _logger.Information($"- User {chatUserId} command updated");

        await _telegramBotClient.SetMyCommands(userCommands.Reverse(),
            new BotCommandScopeChat() { ChatId = chatUserId });
    }

    private async Task<Dictionary<string, string>> IsJoinForce(long chatUserId)
    {
        Dictionary<string, string> linkJoinForce = new();

        foreach (var item in _appSettings.UsernamesForceJoin)
        {
            ChatMember statusJoin = await _telegramBotClient.GetChatMember($"@{item.Key}", chatUserId);

            if (statusJoin.IsAdmin || statusJoin.Status is ChatMemberStatus.Kicked)
            {
                continue;
            }

            if (statusJoin.IsAdmin is false && statusJoin.Status is ChatMemberStatus.Left)
            {
                linkJoinForce.Add(item.Key, item.Value);
            }
        }

        return linkJoinForce;
    }

    private async Task<bool> IsPermissionToUse(long chatUserId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        bool anyUser = await userRepository.Any(chatUserId);

        if (!anyUser)
        {
            return false;
        }

        bool isPeremissionToUse = await userRepository.IsPermisionToUse(chatUserId);

        return isPeremissionToUse;
    }
}