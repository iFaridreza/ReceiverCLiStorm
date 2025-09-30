using RecevicerCliStorm.TelegramBot.Common.Dto;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RecevicerCliStorm.TelegramBot.Bot;

public class TelegramBotApi : ITelegramBotApi
{
    private readonly TelegramBotClient _telegramBotClient;
    private readonly AppSettings _appSettings;
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public TelegramBotApi(TelegramBotClient telegramBotClient, AppSettings appSettings, ILogger logger, IServiceProvider serviceProvider)
    {
        _telegramBotClient = telegramBotClient;
        _appSettings = appSettings;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void Listen()
    {
        throw new NotImplementedException();
    }

    public Task OnMessage(Message message, UpdateType updateType)
    {
        throw new NotImplementedException();
    }

    public Task OnUpdate(Update update)
    {
        throw new NotImplementedException();
    }
}
