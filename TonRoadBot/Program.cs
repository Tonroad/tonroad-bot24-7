using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

var botClient = new TelegramBotClient("ТОКЕН_ТВОЕГО_БОТА"); // Вставь токен
long adminId = 5959529178; // ← Твой Telegram ID

using var cts = new CancellationTokenSource();

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions(),
    cancellationToken: cts.Token
);

Console.WriteLine("🤖 Бот запущен. Ожидаю события...");

await Task.Delay(-1, cts.Token);

// === Обработка сообщений ===
async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message) return;

    // 1. Игрок прислал фото → пересылаем админу
    if (message.Photo != null && message.From.Id != adminId)
    {
        await bot.ForwardMessageAsync(
            chatId: adminId,
            fromChatId: message.Chat.Id,
            messageId: message.MessageId,
            cancellationToken: cancellationToken
        );

        await bot.SendTextMessageAsync(
            chatId: adminId,
            text: $"📷 Фото от игрока {message.From.Id} переслано.",
            cancellationToken: cancellationToken
        );
        return;
    }

    // 2. Админ прислал ответ на фото (reply) с новым фото → бот отправляет обратно игроку
    if (message.Photo != null && message.From.Id == adminId)
    {
        var reply = message.ReplyToMessage;
        if (reply?.ForwardFrom?.Id is long userId)
        {
            string fileId = message.Photo.Last().FileId;

            await bot.SendPhotoAsync(
                chatId: userId,
                photo: new InputOnlineFile(fileId),
                caption: "✅ Вот твоё обработанное фото!",
                cancellationToken: cancellationToken
            );

            await bot.SendTextMessageAsync(
                chatId: adminId,
                text: $"📤 Обработанное фото отправлено игроку {userId}.",
                cancellationToken: cancellationToken
            );
        }
        else
        {
            await bot.SendTextMessageAsync(
                chatId: adminId,
                text: "❌ Сделай *Reply* на фото игрока, чтобы отправить результат.",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }
        return;
    }

    // 3. /start
    if (message.Text == "/start")
    {
        var webAppInfo = new WebAppInfo { Url = "https://tonroad-map.vercel.app" };
        var keyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithWebApp("🌍 Открыть карту", webAppInfo)
        );

        await bot.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: InputFile.FromUri("https://raw.githubusercontent.com/tonroad/tonroad-map/main/tonroad_logo.jpg"),
            caption: "Добро пожаловать в TonRoad!\nНажми кнопку, чтобы открыть карту.",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }
}

// === Обработка ошибок ===
Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"❌ Ошибка: {exception.Message}");
    return Task.CompletedTask;
}
