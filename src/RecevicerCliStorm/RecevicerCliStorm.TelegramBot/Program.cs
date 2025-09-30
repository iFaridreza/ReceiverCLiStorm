using RecevicerCliStorm.TelegramBot.Common;
using RecevicerCliStorm.TelegramBot.Common.Dto;
using RecevicerCliStorm.TelegramBot.Common.Manager;

AppSettings appSettings = Utils.BindConfiguration();

Utils.CreateDir(appSettings.SessionsPath);

ServicesManager.InjectAppSettings(appSettings);
ServicesManager.InjectDatabase(appSettings.DatabaseName);
ServicesManager.InjectRepository();
ServicesManager.InjectTelegramBot(appSettings.Token);
ServicesManager.InjectTelegramLogger(appSettings.Token, appSettings.LogChatId);
ServicesManager.InjectWTelegramFactory();

ServicesManager.BuildServices();


Console.ReadKey();