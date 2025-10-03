using Microsoft.Extensions.DependencyInjection;
using RecevicerCliStorm.TelegramBot.Common;
using RecevicerCliStorm.TelegramBot.Common.Dto;
using RecevicerCliStorm.TelegramBot.Core.Domain;
using RecevicerCliStorm.TelegramBot.Core.IRepository;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = RecevicerCliStorm.TelegramBot.Core.Domain.User;

namespace RecevicerCliStorm.TelegramBot.Bot;

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

            string text = message.Text;

            await _telegramBotClient.SendChatAction(chatUserId, ChatAction.Typing);

            try
            {
                switch (text)
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
                    case "/info":
                    {
                        await OnInfo(chatUserId, messageId);
                    }
                        break;
                    default:
                    {
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

    private async Task OnInfo(long chatUserId, int messageId)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        ISessionRepository sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

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

        _logger.Information($"- User {chatUserId} /info bot");

        IEnumerable<Session> sessions = await sessionRepository.GetAll(chatUserId);

        // ReSharper disable once PossibleMultipleEnumeration
        int countSessionExists = sessions.Count(x => x.ESessionStatus is ESessionStatus.Exists);
        // ReSharper disable once PossibleMultipleEnumeration
        int countSessionSold = sessions.Count(x => x.ESessionStatus is ESessionStatus.Sold);

        ELanguage eLanguageUser = await userRepository.GetLanguage(chatUserId);

        await _telegramBotClient.SendMessage(chatUserId,
            string.Format(Utils.GetText(eLanguageUser, "infoUser"), chatUserId,countSessionExists,countSessionSold), ParseMode.Html,
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

    public Task OnUpdate(Update update)
    {
        throw new NotImplementedException();
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