using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

var botClient = new TelegramBotClient("7740992334:AAHS2q_ogUV7YW1jPg3b5z9FjLtf6fOojwU");

using var cts = new CancellationTokenSource();

// Хранилище соответствия: кто прислал фото — какой file_id
var userPhotoMap = new ConcurrentDictionary<long, string>();

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions { AllowedUpdates = { } },
    cancellationToken: cts.Token
);

Console.WriteLine("Bot started. Running until externally stopped.");

await Task.Delay(-1, cts.Token);

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message) return;

    long senderId = message.Chat.Id;

    // Игрок прислал фото
    if (message.Photo != null && message.Photo.Length > 0)
    {
        var photo = message.Photo.Last(); // самое большое фото
        string fileId = photo.FileId;

        // Сохраняем соответствие: игрок → фото
        userPhotoMap[senderId] = fileId;

        // Пересылаем фото тебе (5959529178)
        await bot.SendTextMessageAsync(
            chatId: 5959529178,
            text: $"📸 Игрок `{senderId}` прислал фото.",
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );

        await bot.SendPhotoAsync(
            chatId: 5959529178,
            photo: InputFile.FromFileId(fileId),
            caption: $"Для ответа используй команду:\n`/sendback {senderId}`",
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );

        return;
    }

    // Команда от тебя: /sendback <id>
    if (message.Text != null && message.Text.StartsWith("/sendback"))
    {
        var parts = message.Text.Split(' ');
        if (parts.Length == 2 && long.TryParse(parts[1], out long targetId))
        {
            if (userPhotoMap.TryGetValue(targetId, out string fileId))
            {
                await bot.SendPhotoAsync(
                    chatId: targetId,
                    photo: InputFile.FromFileId(fileId),
                    caption: "✅ Вот твоё обработанное фото!",
                    cancellationToken: cancellationToken
                );

                await bot.SendTextMessageAsync(
                    chatId: 5959529178,
                    text: "Фото отправлено обратно игроку.",
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await bot.SendTextMessageAsync(
                    chatId: 5959529178,
                    text: "❌ Фото для этого пользователя не найдено.",
                    cancellationToken: cancellationToken
                );
            }
        }
    }

    // /start — показать кнопку карты
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
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"❌ Ошибка: {exception.Message}");
    return Task.CompletedTask;
}
