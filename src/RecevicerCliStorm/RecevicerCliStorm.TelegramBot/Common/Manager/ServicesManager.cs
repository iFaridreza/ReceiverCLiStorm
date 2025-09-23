using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecevicerCliStorm.TelegramBot.Core.IRepository;
using RecevicerCliStorm.TelegramBot.Infrastructer;

namespace RecevicerCliStorm.TelegramBot.Common.Manager;

public static class ServicesManager
{
    private static IServiceCollection _serviceCollection;
    static ServicesManager() => _serviceCollection = new ServiceCollection();

    public static void Database(string databaseName)
    {
        SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new() { DataSource = databaseName };

        _serviceCollection.AddDbContext<Context>(x => x.UseSqlite(sqliteConnectionStringBuilder.ConnectionString),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);
    }

    public static void RepositoryScopedServices()
    {

    }
}
