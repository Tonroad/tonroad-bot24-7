using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

var botClient = new TelegramBotClient("ТОКЕН_ТВОЕГО_БОТА"); // Замени на свой токен
var adminId = 5959529178; // ← Твой Telegram ID

using var cts = new CancellationTokenSource();

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions { AllowedUpdates = { } },
    cancellationToken: cts.Token
);

Console.WriteLine("🤖 Бот запущен. Ожидаю события...");

await Task.Delay(-1, cts.Token);

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;

    // /start команда
    if (message.Text == "/start")
    {
        var webAppInfo = new WebAppInfo { Url = "https://tonroad-map.vercel.app" };
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithWebApp("🌍 Open TonRoad Map", webAppInfo)
        });

        await bot.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: InputFile.FromUri("https://raw.githubusercontent.com/tonroad/tonroad-map/main/tonroad_logo.jpg"),
            caption: "Добро пожаловать в TonRoad!\nНажмите кнопку ниже, чтобы открыть карту.",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
        return;
    }

    // Пользователь отправил фото → пересылаем админу
    if (message.Photo != null && message.From.Id != adminId)
    {
        await bot.ForwardMessageAsync(
            chatId: adminId,
            fromChatId: message.Chat.Id,
            messageId: message.MessageId,
            cancellationToken: cancellationToken
        );

        Console.WriteLine($"📷 Фото от пользователя {message.From.Id} переслано админу.");
        return;
    }

    // Админ ответил на фото → отправляем обратно пользователю
    if (message.Photo != null && message.From.Id == adminId)
    {
        var reply = message.ReplyToMessage;
        if (reply?.ForwardFrom?.Id is long userId)
        {
            var fileId = message.Photo.Last().FileId;

            await bot.SendPhotoAsync(
                chatId: userId,
                photo: new InputOnlineFile(fileId),
                caption: "Ваше обработанное фото готово!",
                cancellationToken: cancellationToken
            );

            Console.WriteLine($"📤 Фото от админа отправлено пользователю {userId}");
        }
        else
        {
            await bot.SendTextMessageAsync(
                chatId: adminId,
                text: "⚠️ Пожалуйста, ответьте (Reply) на фото, присланное пользователем.",
                cancellationToken: cancellationToken
            );
        }
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"❌ Ошибка: {exception.Message}");
    return Task.CompletedTask;
}
