using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecevicerCliStorm.TelegramBot.Bot;
using RecevicerCliStorm.TelegramBot.Common.Dto;
using RecevicerCliStorm.TelegramBot.Core.IRepository;
using RecevicerCliStorm.TelegramBot.Infrastructer;
using RecevicerCliStorm.TelegramBot.Infrastructer.Repository;
using RecevicerCliStorm.TelegramBot.WTelegramClientUtils;
using Serilog;
using Serilog.Sinks.TelegramBot;
using Telegram.Bot;

namespace RecevicerCliStorm.TelegramBot.Common.Manager;

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
    }

    public static void InjectWTelegramFactory()
    {
        _serviceCollection.AddScoped<IWTelegramClientManagerFactory, WTelegramClientManagerFactory>();
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
