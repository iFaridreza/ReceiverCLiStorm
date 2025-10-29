using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ReceiverCliStorm.TelegramBot.Common.Dto;
using ReceiverCliStorm.TelegramBot.Core.Domain;
using ReceiverCliStorm.TelegramBot.Core.IRepository;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ReceiverCliStorm.TelegramBot.Common.Trigger;

public class StepTrigger : IJob
{
    private readonly TelegramBotClient _telegramBot;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppSettings _appSettings;
    // ReSharper disable once InconsistentNaming
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

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();

            await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
            IUserStepRepository userStepRepository = scope.ServiceProvider.GetRequiredService<IUserStepRepository>();
            ISudoRepository sudoRepository = scope.ServiceProvider.GetRequiredService<ISudoRepository>();
            ILogger logger = scope.ServiceProvider.GetRequiredService<ILogger>();

            IEnumerable<UserStep> userSteps = await userStepRepository.GetAll();
            
            DateTime dateNow = DateTime.Now;
            
            IEnumerable<UserStep> expiredSteps = userSteps.Where(x => x.ExpierDateTime <= dateNow).ToList();

            foreach (UserStep item in expiredSteps)
            {
                await userStepRepository.Remove(item.ChatId);
                
                try
                {
                    bool anySudo = await sudoRepository.Any(item.ChatId);

                    if (!anySudo)
                    {
                        continue;
                    }

                    ELanguage eLanguage = await sudoRepository.GetLanguage(item.ChatId);

                    await _telegramBot.SendMessage(item.ChatId, string.Format(Utils.GetText(eLanguage, "timeOut"), _appSettings.AskTimeOutMinute),ParseMode.Html);

                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Exception StepTrigger");
                }
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}