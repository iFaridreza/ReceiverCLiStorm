using Quartz;
using RecevicerCliStorm.TelegramBot.Common.Dto;
using Telegram.Bot;

namespace RecevicerCliStorm.TelegramBot.Common.Trigger;

public class StepTrigger : IJob
{
    private readonly TelegramBotClient _telegramBot;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppSettings _appSettings;
    private static readonly SemaphoreSlim _semaphoreSlim;

    static StepTrigger()
    {
        _semaphoreSlim = new(1, 1);
    }

    public StepTrigger(TelegramBotClient telegramBot, IServiceProvider serviceProvider, AppSettings appSettings)
    {
        _telegramBot = telegramBot;
        _serviceProvider = serviceProvider;
        _appSettings = appSettings;
    }

    public Task Execute(IJobExecutionContext context)
    {
        throw new NotImplementedException();
    }
}