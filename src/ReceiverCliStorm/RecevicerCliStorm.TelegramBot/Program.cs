using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using ReceiverCliStorm.TelegramBot.Bot;
using ReceiverCliStorm.TelegramBot.Common;
using ReceiverCliStorm.TelegramBot.Common.Dto;
using ReceiverCliStorm.TelegramBot.Common.Manager;
using ReceiverCliStorm.TelegramBot.Core.IRepository;
using ReceiverCliStorm.TelegramBot.Infrastructer;

AppSettings appSettings = Utils.BindConfiguration();

Utils.CreateDir(appSettings.SessionsPath);

ServicesManager.InjectAppSettings(appSettings);
ServicesManager.InjectDatabase(appSettings.DatabaseName);
ServicesManager.InjectRepository();
ServicesManager.InjectTelegramBot(appSettings.Token);
ServicesManager.InjectTelegramLogger(appSettings.Token, appSettings.LogChatId);
ServicesManager.InjectWTelegramFactory();
ServicesManager.InjectStepTrigger();

IServiceProvider serviceProvider = ServicesManager.BuildServices();

using IServiceScope serviceScope = serviceProvider.CreateScope();

Context context = serviceScope.ServiceProvider.GetRequiredService<Context>();
context.Database.EnsureCreated();

ISudoRepository sudoRepository = serviceScope.ServiceProvider.GetRequiredService<ISudoRepository>();
ISettingsRepository settingsRepository = serviceScope.ServiceProvider.GetRequiredService<ISettingsRepository>();
ISessionInfoRepository sessionInfoRepository = serviceScope.ServiceProvider.GetRequiredService<ISessionInfoRepository>();

foreach (long sudo in appSettings.Sudos)
{
    bool any = await sudoRepository.Any(sudo);

    if (!any)
    {
        await sudoRepository.Create(new()
        {
            ChatId = sudo
        });
    }
}

bool anySettings = await settingsRepository.Any();

if (!anySettings)
{
    await settingsRepository.Create(new()
    {
        UseProxy = appSettings.UseProxy,
        UseChangeBio = appSettings.UseChangeBio,
        UseCheckReport = appSettings.UseCheckReport,
        UseLogCLI = appSettings.UseLogCLI,
    });
}

bool anySessionInfo = await sessionInfoRepository.Any();

if (!anySessionInfo)
{
    await sessionInfoRepository.Create(new()
    {
        ApiHash = appSettings.ApiHash,
        ApiId = appSettings.ApiId
    });
}

LanguageManager.SetEn(appSettings.EnPath);
LanguageManager.SetFa(appSettings.FaPath);

ITelegramBotApi telegramBotApi = serviceScope.ServiceProvider.GetRequiredService<ITelegramBotApi>();

telegramBotApi.Listen();

ISchedulerFactory schedulerFactory = serviceScope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
IScheduler scheduler = await schedulerFactory.GetScheduler();
scheduler.JobFactory = serviceScope.ServiceProvider.GetRequiredService<IJobFactory>();
await scheduler.Start();

Console.ReadKey(false);