using Microsoft.Extensions.DependencyInjection;
using ReceiverCliStorm.TelegramBot.Common;
using ReceiverCliStorm.TelegramBot.Common.Dto;
using ReceiverCliStorm.TelegramBot.Common.Manager;
using ReceiverCliStorm.TelegramBot.Core.Domain;
using ReceiverCliStorm.TelegramBot.Core.IRepository;
using ReceiverCliStorm.TelegramBot.WTelegramClientUtils;
using Serilog;
using System.Diagnostics;
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
        ISessionInfoRepository sessionInfoRepository = scope.ServiceProvider.GetRequiredService<ISessionInfoRepository>();
        ISettingsRepository settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        IWTelegramClientManagerFactory wTelegramClientManagerFactory = scope.ServiceProvider.GetRequiredService<IWTelegramClientManagerFactory>();

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

            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "invalidPhone"), ParseMode.Html,
            replyParameters: messageId);

            return;
        }

        string sessionPath = Path.Combine(AppContext.BaseDirectory, _appSettings.SessionsPath, string.Concat(phoneNumber, ".session"));

        InfoPhoneNumber infoPhoneNumber = Utils.InfoPhoneNumber(phoneNumber);

        bool anyExistsSessionDb = await sessionRepository.Any(infoPhoneNumber.CountryCode, infoPhoneNumber.PhoneNumber);
        bool anyExistsSessionFile = Utils.AnySessions(sessionPath);

        if (anyExistsSessionDb || anyExistsSessionDb)
        {
            _logger.Information($"- User {chatUserId} Exists Phone {phoneNumber} Bot");

            await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "existsPhone"), ParseMode.Html,
            replyParameters: messageId);

            return;
        }


        Message msgWiteProsessing = await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "waite"), ParseMode.Html,
            replyParameters: messageId);

        Settings settings = await settingsRepository.GetSingleFirst();
        SessionInfo sessionInfo = await sessionInfoRepository.GetSingleFirst();

        Action<int, string> loging = (_, _) => { };

        if (settings.UseLogCLI)
        {
            loging = (_, msg) =>
            {
                _logger.Information($"- Log Session : {phoneNumber}\n\n{msg}");
            };
        }

        IWTelegramClientManager wTelegramClientManager = wTelegramClientManagerFactory
            .Create(
                sessionInfo.ApiId,
                sessionInfo.ApiHash,
                sessionPath,
                loging
                );
        try
        {
            await wTelegramClientManager.Connect();

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

                        wTelegramClientManager.UseScokcs5Proxy(proxy.Ip, port, proxy.Username, proxy.Password);

                        break;
                    }
                }

                if (!isConnected)
                {
                    _logger.Warning($"- Log Session : \n{phoneNumber}\n\nCant Connected Proxy");
                }
            }

            string state = await wTelegramClientManager.Login(phoneNumber);

            if (string.IsNullOrEmpty(state) || state != "verification_code")
            {
                _logger.Information($"- User {chatUserId} Login Session {phoneNumber} State: {state} Bot");

                await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId, Utils.GetText(eLanguageUser, "tryAgain"));

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

            SessionCasheManager.AddOrUpdate(chatUserId, wTelegramClientManager);

            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId, string.Format(Utils.GetText(eLanguageUser, "loginCode"), phoneNumber));
        }
        catch (Exception ex) when (ex.Message == "PHONE_NUMBER_INVALID")
        {
            _logger.Information($"- User {chatUserId} Login Session {phoneNumber} Invalid Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId, string.Format(Utils.GetText(eLanguageUser, "phoneInvalid"), phoneNumber));
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionPath);
        }
        catch (Exception ex) when (ex.Message == "PHONE_NUMBER_FLOOD")
        {
            _logger.Information($"- User {chatUserId} Login Session {phoneNumber} Flood Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId, string.Format(Utils.GetText(eLanguageUser, "phoneFlood"), phoneNumber));
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionPath);
        }
        catch (Exception ex) when (ex.Message == "PHONE_NUMBER_BANNED")
        {
            _logger.Information($"- User {chatUserId} Login Session {phoneNumber} Ban Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId, string.Format(Utils.GetText(eLanguageUser, "phoneBan"), phoneNumber));
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionPath);
        }
        catch (Exception ex) when (
            ex.Message.Contains("BadMsgNotification") ||
            ex.Message.Contains("Connection shut down") ||
            ex.Message.Contains("TimedOut") ||
            ex.Message.Contains("A connection attempt failed"))
        {
            _logger.Warning(ex,$"- User {chatUserId} Login Session {phoneNumber} Bot");
            await _telegramBotClient.EditMessageText(chatUserId, msgWiteProsessing.MessageId, Utils.GetText(eLanguageUser, "tryAgain"));
            await wTelegramClientManager.Disconnect();
            File.Delete(sessionPath);
        }
    }

    private async Task OnHelp(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        ISessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        IUserStepRepository userStepRepository = scope.ServiceProvider.GetRequiredService<IUserStepRepository>();

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

        _logger.Information($"- User {chatUserId} /help bot");

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        await _telegramBotClient.SendMessage(chatUserId, Utils.GetText(eLanguageUser, "help"), ParseMode.Html,
           replyParameters: messageId);
    }

    public async Task OnUpdate(Update update)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (update.CallbackQuery is null ||
                    update.CallbackQuery.Message is null ||
                    update.CallbackQuery.Message.Chat.Type != ChatType.Private)
                {
                    return;
                }

                long chatUserId = update.CallbackQuery.Message.Chat.Id;
                int messageId = update.CallbackQuery.Message.MessageId;

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
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, nameof(Exception));
            }
        });

        await Task.CompletedTask;
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

        Message msgRemoveButtotn =
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

        await _telegramBotClient.EditMessageText(chatUserId, msgRemoveButtotn.MessageId,
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
                        Utils.GetText(eLanguageSudo, "changePeremission")));
            }

            return;
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

        //Note Delete all step user & cashe remove all 

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

            if (_appSettings.CommandsUser.Count == default)
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

        //Note Delete all step user & cashe remove all 

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
        catch
        {
            throw;
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

        User user = await userRepository.Get(userChatId);

        if (user.IsPermissionToUse)
        {
            await userRepository.UnauthorizedPermisionToUse(user);
        }
        else
        {
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
                Utils.GetText(eLanguageSudo, "changePeremission")));
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