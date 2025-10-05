using System.Text.Json;

namespace RecevicerCliStorm.TelegramBot.Common.Manager;

public static class LanguageManager
{
    static IDictionary<string, string> En { get; set; }
    static IDictionary<string, string> Fa { get; set; }

    static LanguageManager()
    {
        En = new Dictionary<string, string>();
        Fa = new Dictionary<string, string>();
    }

    public static bool HaveData(string pathEn)
    {
        string data = File.ReadAllText(pathEn);

        return !string.IsNullOrEmpty(data);
    }

    public static string GetEnValue(string key)
    {
        if (!En.TryGetValue(key, out string? value))
        {
            throw new KeyNotFoundException($"{nameof(key)} {key}");
        }

        return value;
    }

    public static string GetFaValue(string key)
    {
        if (!Fa.TryGetValue(key, out string? value))
        {
            throw new KeyNotFoundException($"{nameof(key)} {key}");
        }

        return value;
    }

    public static void SetEn(string pathEn)
    {
        string data = File.ReadAllText(pathEn);

        Dictionary<string, string>? dataDict = JsonSerializer.Deserialize<Dictionary<string, string>>(data);

        En = dataDict ?? throw new NullReferenceException(nameof(pathEn));
    }

    public static void SetFa(string pathFa)
    {
        string data = File.ReadAllText(pathFa);

        Dictionary<string, string>? dataDict = JsonSerializer.Deserialize<Dictionary<string, string>>(data);

        Fa = dataDict ?? throw new NullReferenceException(nameof(pathFa));
    }

}