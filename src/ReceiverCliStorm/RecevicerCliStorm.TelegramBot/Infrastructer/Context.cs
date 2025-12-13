using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReceiverCliStorm.TelegramBot.Core.Domain;

namespace ReceiverCliStorm.TelegramBot.Infrastructer;

public class Context : DbContext
{
    public Context(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Sudo> Sudo { get; set; }
    public DbSet<User> User { get; set; }
    public DbSet<Session> Session { get; set; }
    public DbSet<SessionInfo> SessionInfo { get; set; }
    public DbSet<UserStep> UserStep { get; set; }
    public DbSet<Settings> Settings { get; set; }

    public DbSet<DeviceAuthInfo> DeviceAuthInfo { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var sudoBuilder = modelBuilder.Entity<Sudo>();
        sudoBuilder.ToTable("Sudo");
        sudoBuilder.HasKey(x => x.Id);
        sudoBuilder.HasIndex(x => x.ChatId).IsUnique();
        sudoBuilder.Property(x => x.ChatId).HasMaxLength(50).IsRequired();
        sudoBuilder.Property(x => x.Language).HasConversion(new EnumToStringConverter<ELanguage>())
            .HasDefaultValue(ELanguage.En);


        var userBuilder = modelBuilder.Entity<User>();
        userBuilder.ToTable("User");
        userBuilder.HasKey(x => x.Id);
        userBuilder.HasIndex(x => x.ChatId).IsUnique();
        userBuilder.Property(x => x.ChatId).HasMaxLength(50).IsRequired();
        userBuilder.Property(x => x.IsPermissionToUse).IsRequired().HasDefaultValue(false);
        userBuilder.Property(x => x.Language).HasConversion(new EnumToStringConverter<ELanguage>())
            .HasDefaultValue(ELanguage.En);


        var sessionBuilder = modelBuilder.Entity<Session>();
        sessionBuilder.ToTable("Session");
        sessionBuilder.HasKey(x => x.Id);
        sessionBuilder.HasIndex(x => new
        {
            x.CountryCode,
            x.Number
        }).IsUnique();
        sessionBuilder.Property(x => x.CountryCode).HasMaxLength(5).IsRequired();
        sessionBuilder.Property(x => x.Number).IsRequired();
        sessionBuilder.Property(x => x.RegisterDate).IsRequired();
        sessionBuilder.Property(x => x.SessionStatus).HasConversion(new EnumToStringConverter<ESessionStatus>())
            .HasDefaultValue(ESessionStatus.Exists);
        sessionBuilder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        sessionBuilder.HasOne(x => x.SessionInfo).WithMany().HasForeignKey(x => x.SessionInfoId);
        sessionBuilder.HasOne(x => x.DeviceAuthInfo).WithOne().HasForeignKey<DeviceAuthInfo>(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        
        var sessionInfoBuilder = modelBuilder.Entity<SessionInfo>();
        sessionInfoBuilder.ToTable("SessionInfo");
        sessionInfoBuilder.HasKey(x => x.Id);
        sessionInfoBuilder.Property(x => x.ApiId).IsRequired();
        sessionInfoBuilder.Property(x => x.ApiHash).IsRequired();

        var deviceAuthInfoBuilder = modelBuilder.Entity<DeviceAuthInfo>();
        deviceAuthInfoBuilder.ToTable("DeviceAuthInfo");
        deviceAuthInfoBuilder.HasKey(x => x.Id);
        deviceAuthInfoBuilder.Property(x => x.DeviceModel).HasMaxLength(100).IsRequired();
        deviceAuthInfoBuilder.Property(x => x.SystemVersion).HasMaxLength(50).IsRequired();
        deviceAuthInfoBuilder.Property(x => x.AppVersion).HasMaxLength(20).IsRequired();
        deviceAuthInfoBuilder.Property(x => x.LangCode).HasMaxLength(2).IsRequired();

        var userStepBuilder = modelBuilder.Entity<UserStep>();
        userStepBuilder.ToTable("UserStep");
        userStepBuilder.HasKey(x => x.Id);
        userStepBuilder.HasIndex(x => x.ChatId).IsUnique();
        userStepBuilder.Property(x => x.Step).HasMaxLength(100).IsRequired();
        userStepBuilder.Property(x => x.ExpierDateTime).IsRequired();
        userStepBuilder.Property(x => x.ChatId).HasMaxLength(50).IsRequired();


        var settingsBuilder = modelBuilder.Entity<Settings>();
        settingsBuilder.ToTable("Settings");
        settingsBuilder.HasKey(x => x.Id);
        settingsBuilder.Property(x => x.UseProxy).IsRequired();
        settingsBuilder.Property(x => x.UseChangeBio).IsRequired();
        settingsBuilder.Property(x => x.UseCheckReport).IsRequired();
        settingsBuilder.Property(x => x.UseLogCLI).IsRequired();
    }
}