using RecevicerCliStorm.TelegramBot.Common;
using RecevicerCliStorm.TelegramBot.Common.Dto;

AppSettings appSettings = Utils.BindConfiguration();

Utils.CreateDir(appSettings.SessionsPath);

Console.ReadKey();