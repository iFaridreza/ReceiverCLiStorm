using ReceiverCliStorm.TelegramBot.Core.Domain;
using Telegram.Bot.Types.ReplyMarkups;

namespace ReceiverCliStorm.TelegramBot.Common;

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

    public static InlineKeyboardMarkup Reload(string textConfirmReload)
    {
        InlineKeyboardMarkup replyKeyboardMarkup = new();

        replyKeyboardMarkup.AddButton(new()
        {
            Text = textConfirmReload,
            CallbackData = "Reload"
        }).AddNewRow();

        replyKeyboardMarkup.AddButton(new()
        {
            Text = "🔙",
            CallbackData = "Back"
        });

        return replyKeyboardMarkup;
    }

    public static InlineKeyboardMarkup Settings(Settings settings, string textButtonUseProxy, string textButtonUseChangeBio, string textButtonUseCheckReport, string textButtonUseLogCli)
    {
        InlineKeyboardMarkup inlineKeyboardMarkup = new();

        inlineKeyboardMarkup.AddButtons([

            new()
            {
                Text = textButtonUseProxy,
                CallbackData = "Alert"
            },

            new()
            {
                Text = settings.UseProxy ? "✅" : "❌",
                CallbackData = "UseProxy"
            }

        ]).AddNewRow();

        inlineKeyboardMarkup.AddButtons([

            new()
            {
                Text = textButtonUseChangeBio,
                CallbackData = "Alert"
            },

            new()
            {
                Text = settings.UseChangeBio ? "✅" : "❌",
                CallbackData = "UseChangeBio"
            }
        ]).AddNewRow();

        inlineKeyboardMarkup.AddButtons([

          new()
            {
                Text = textButtonUseCheckReport,
                CallbackData = "Alert"
            },

            new()
            {
                Text = settings.UseCheckReport ? "✅" : "❌",
                CallbackData = "UseCheckReport"
            }
         ]).AddNewRow();

        inlineKeyboardMarkup.AddButtons([

         new()
            {
                Text = textButtonUseLogCli,
                CallbackData = "Alert"
            },

            new()
            {
                Text = settings.UseLogCLI ? "✅" : "❌",
                CallbackData = "UseLogCLi"
            }
        ]);

        return inlineKeyboardMarkup;

    }

    public static InlineKeyboardMarkup StatusPermisionUser(long chatUserId, string textButtonPermisionUser,string textButtonBackupUserSessions)
    {
        InlineKeyboardMarkup inlineKeyboardMarkup = new();

        inlineKeyboardMarkup.AddButton(new()
        {
            Text = textButtonBackupUserSessions,
            CallbackData = $"BackupSessions_{chatUserId}"
        }).AddNewRow();
        
        inlineKeyboardMarkup.AddButton(new()
        {
            Text = textButtonPermisionUser,
            CallbackData = $"ChangePermission_{chatUserId}"
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
    
    public static InlineKeyboardMarkup DownloadCurrentSession(string textDownloadCurrentSession,string phoneNumber)
    {
        InlineKeyboardMarkup replyKeyboardMarkup = new();

        replyKeyboardMarkup.AddButton(new()
        {
            Text = textDownloadCurrentSession,
            CallbackData = $"Download_{phoneNumber}"
        });
        
        return replyKeyboardMarkup;
    }
}
