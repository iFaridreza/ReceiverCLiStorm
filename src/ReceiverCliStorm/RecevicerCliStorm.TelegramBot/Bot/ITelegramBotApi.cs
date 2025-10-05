using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RecevicerCliStorm.TelegramBot.Bot;

public interface ITelegramBotApi
{
    void Listen();
    Task OnMessage(Message message, UpdateType updateType);
    Task OnUpdate(Update update);
}
