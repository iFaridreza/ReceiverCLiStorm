using Microsoft.Extensions.Configuration;
using PhoneNumbers;
using System.Runtime.CompilerServices;
using ReceiverCliStorm.TelegramBot.Common.Dto;
using ReceiverCliStorm.TelegramBot.Common.Manager;
using ReceiverCliStorm.TelegramBot.Core.Domain;

namespace ReceiverCliStorm.TelegramBot.Common;

public static class Utils
{   
    public static AppSettings BindConfiguration()
    {
        const string fileName = "appsettings.json";

        IConfiguration configuration = new ConfigurationBuilder().AddJsonFile(fileName).SetBasePath(AppContext.BaseDirectory).Build();

        AppSettings appSettings = new();

        configuration.Bind(appSettings);

        return appSettings;
    }
    
    public static void CreateDir(string dir)
    {
        if (Directory.Exists(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
    }

    public static bool AnySessions(string sessionsPath) => File.Exists(sessionsPath);

    public static string GetText(ELanguage eLanguage, string key)
    {
        string text = eLanguage switch
        {
            ELanguage.Fa => LanguageManager.GetFaValue(key),
            _ => LanguageManager.GetEnValue(key)
        };

        return text;
    }

    public static DateTime GetDateTime(long minute)
    {
        DateTime dateTimeNow = DateTime.Now;

        DateTime roundTime = new(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, dateTimeNow.Hour, dateTimeNow.Minute, dateTimeNow.Second);

        return roundTime.AddMinutes(minute);
    }

    public static DateTime GetDateTime()
    {
        DateTime dateTimeNow = DateTime.Now;

        DateTime roundTime = new(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 0, 0, 0);

        return roundTime;
    }

    public static InfoPhoneNumber InfoPhoneNumber(string phoneNumber)
    {
        PhoneNumberUtil phoneNumberInit = PhoneNumberUtil.GetInstance();

        PhoneNumber number = phoneNumberInit.Parse(phoneNumber, null);

        string regionCode = phoneNumberInit.GetRegionCodeForNumber(number);

        string regionCountry = $"+{phoneNumberInit.GetCountryCodeForRegion(regionCode)}";

        string formattedNumber = phoneNumberInit.Format(number, PhoneNumberFormat.E164);

        return new InfoPhoneNumber()
        {
            CountryCode = regionCountry,
            ShortCountryCode = regionCode,
            PhoneNumber = formattedNumber.Replace(regionCountry, string.Empty)
        };
    }
}