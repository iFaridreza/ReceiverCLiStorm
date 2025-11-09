using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ReceiverCliStorm.TelegramBot.Bot;
using ReceiverCliStorm.TelegramBot.Common.Dto;
using ReceiverCliStorm.TelegramBot.Common.Trigger;
using ReceiverCliStorm.TelegramBot.Core.IRepository;
using ReceiverCliStorm.TelegramBot.Infrastructer;
using ReceiverCliStorm.TelegramBot.Infrastructer.Repository;
using ReceiverCliStorm.TelegramBot.WTelegramUtils;
using Serilog;
using Serilog.Sinks.TelegramBot;
using Telegram.Bot;

namespace ReceiverCliStorm.TelegramBot.Common.Manager;

public static class ServicesManager
{
    private static IServiceCollection _serviceCollection;
    static ServicesManager() => _serviceCollection = new ServiceCollection();

    public static void InjectDatabase(string databaseName)
    {
        SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new() { DataSource = databaseName };

        _serviceCollection.AddDbContext<Context>(x => x.UseSqlite(sqliteConnectionStringBuilder.ConnectionString),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);
    }

    public static void InjectRepository()
    {
        _serviceCollection.AddScoped<IUserStepRepository, UserStepRepository>();
        _serviceCollection.AddScoped<IUserRepository, UserRepository>();
        _serviceCollection.AddScoped<ISudoRepository, SudoRepository>();
        _serviceCollection.AddScoped<ISessionRepository, SessionRepository>();
        _serviceCollection.AddScoped<ISessionInfoRepository, SessionInfoRepository>();
        _serviceCollection.AddScoped<ISettingsRepository, SettingsRepository>();
    }

    public static void InjectWTelegramFactory()
    {
        _serviceCollection.AddScoped<IWTelegramManagerFactory, WTelegramManagerFactory>();
    }

    public static void InjectTelegramLogger(string token, string chatIdLog)
    {
        _serviceCollection.AddSingleton<ILogger>(x =>
        {
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TelegramBot(token, chatIdLog)
                .CreateLogger();
        });
    }

    public static void InjectStepTrigger()
    {
        _serviceCollection.AddLogging();

        _serviceCollection.AddQuartz(x =>
        {
            JobKey jobKey = new("StepTrigger");
            x.AddJob<StepTrigger>(opts => opts.WithIdentity(jobKey));

            x.AddTrigger(opts =>
                opts.ForJob(jobKey).WithIdentity("Trigger-Step")
                .WithSimpleSchedule(y => y.WithIntervalInMinutes(1)
                .RepeatForever())
                );
        });

        _serviceCollection.AddQuartzHostedService(x => x.WaitForJobsToComplete = true);
    }

    public static void InjectTelegramBot(string token)
    {
        _serviceCollection.AddSingleton(x =>
        {
            return new TelegramBotClient(token);
        });

        _serviceCollection.AddSingleton<ITelegramBotApi, TelegramBotApi>();
    }


    public static void InjectAppSettings(AppSettings appSettings)
    {
        _serviceCollection.AddSingleton(x =>
        {
            return appSettings;
        });
    }

    public static IServiceProvider BuildServices() => _serviceCollection.BuildServiceProvider();
}
