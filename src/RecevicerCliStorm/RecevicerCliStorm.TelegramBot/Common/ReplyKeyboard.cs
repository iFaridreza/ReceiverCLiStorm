using Telegram.Bot.Types.ReplyMarkups;

namespace RecevicerCliStorm.TelegramBot.Common;

public static class ReplyKeyboard
{
    public static InlineKeyboardMarkup JoinForce(Dictionary<string, string> usernameJoinForces, string textButtonIJoin)
    {
        InlineKeyboardMarkup inlineKeyboardMarkup = new();

        foreach (var item in usernameJoinForces)
        {
            inlineKeyboardMarkup.AddButton(new()
            {
                Text = item.Value,
                Url = $"https://t.me/{item.Key}"
            }).AddNewRow();
        }

        inlineKeyboardMarkup.AddButton(new()
        {
            Text = textButtonIJoin,
            CallbackData = $"IJoin"
        }).AddNewRow();

        return inlineKeyboardMarkup;
    }

    public static InlineKeyboardMarkup Developer(string textButtonDeveloper, string developerUsername)
    {
        InlineKeyboardMarkup replyKeyboardMarkup = new();

        replyKeyboardMarkup.AddButton(new()
        {
            Text = textButtonDeveloper,
            Url = $"https://t.me/{developerUsername}"
        });

        return replyKeyboardMarkup;
    }
}
