using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var botClient = new TelegramBotClient("7740992334:AAHS2q_ogUV7YW1jPg3b5z9FjLtf6fOojwU");

using var cts = new CancellationTokenSource();

var userMap = new Dictionary<int, long>(); // messageId -> original userId

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions { AllowedUpdates = { } },
    cancellationToken: cts.Token
);

Console.WriteLine("Bot started. Running until externally stopped.");
await Task.Delay(-1, cts.Token);

// ------------------ Обработка входящих сообщений ------------------

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    var message = update.Message;
    if (message == null) return;

    // Игрок отправил команду /start
    if (message.Text == "/start")
    {
        var webAppInfo = new WebAppInfo
        {
            Url = "https://tonroad-map.vercel.app"
        };

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
    }

    // Фото от игрока → пересылаем админу (тебе)
    else if (message.Photo != null && message.Chat.Id != 5959529178)
    {
        var fileId = message.Photo.Last().FileId;

        userMap[message.MessageId] = message.Chat.Id;

        await bot.SendTextMessageAsync(
            chatId: 5959529178,
            text: $"📷 Получено фото от @{message.Chat.Username ?? "без ника"} (ID: {message.Chat.Id})"
        );

        await bot.SendPhotoAsync(
            chatId: 5959529178,
            photo: InputFile.FromFileId(fileId),
            caption: $"🔁 Чтобы отправить ответ, просто пришли фото с подписью вида:\n`reply {message.MessageId}`",
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );
    }

    // Ты (админ) отправляешь фото обратно игроку с подписью "reply 123"
    else if (message.Photo != null && message.Chat.Id == 5959529178 && message.Caption?.StartsWith("reply ") == true)
    {
        var parts = message.Caption.Split(' ');
        if (parts.Length >= 2 && int.TryParse(parts[1], out int replyId) && userMap.TryGetValue(replyId, out long originalUserId))
        {
            await bot.SendPhotoAsync(
                chatId: originalUserId,
                photo: InputFile.FromFileId(message.Photo.Last().FileId),
                caption: "Вот ваш мультяшный аватар! Спасибо 😎",
                cancellationToken: cancellationToken
            );

            await bot.SendTextMessageAsync(
                chatId: 5959529178,
                text: "✅ Ответ отправлен игроку."
            );
        }
        else
        {
            await bot.SendTextMessageAsync(
                chatId: 5959529178,
                text: "⚠️ Не удалось найти игрока по message ID."
            );
        }
    }
}

// ------------------ Обработка ошибок ------------------

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"Ошибка: {exception.Message}");
    return Task.CompletedTask;
}
