using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RecevicerCliStorm.TelegramBot.Common;
using RecevicerCliStorm.TelegramBot.Common.Dto;

namespace RecevicerCliStorm.TelegramBot.Infrastructer;

public class ContextFactory : IDesignTimeDbContextFactory<Context>
{
    public Context CreateDbContext(string[] args)
    {
        AppSettings appSettings = Utils.BindConfiguration();

        var optionsBuilder = new DbContextOptionsBuilder<Context>();

        string basePath = AppContext.BaseDirectory;

        string dbPath = Path.Combine(basePath, appSettings.DatabaseName);

        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = dbPath
        };

        optionsBuilder.UseSqlite(connectionStringBuilder.ConnectionString);

        return new Context(optionsBuilder.Options);
    }
}