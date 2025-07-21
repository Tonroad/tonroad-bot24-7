using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

var botClient = new TelegramBotClient("7740992334:AAHS2q_ogUV7YW1jPg3b5z9FjLtf6fOojwU");
using var cts = new CancellationTokenSource();

const long AdminId = 5959529178; // твой Telegram user id

// Хранилище: кто прислал фото — file_id
var userPhotoMap = new ConcurrentDictionary<long, string>();
// Очередь ожидания: кому отправить следующее фото от админа
var pendingSendbacks = new ConcurrentDictionary<long, long>();

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

    // 1. Команда от админа: /sendback <id>
    if (message.Text != null && message.Text.StartsWith("/sendback") && senderId == AdminId)
    {
        var parts = message.Text.Split(' ');
        if (parts.Length == 2 && long.TryParse(parts[1], out long targetId))
        {
            // Включаем режим ожидания фото
            pendingSendbacks[senderId] = targetId;

            await bot.SendTextMessageAsync(
                chatId: AdminId,
                text: $"Теперь отправь фото, которое хочешь переслать игроку {targetId}.",
                cancellationToken: cancellationToken
            );
        }
        return;
    }

    // 2. Фото от админа (если ждём)
    if (message.Photo != null && message.Photo.Length > 0 && senderId == AdminId)
    {
        if (pendingSendbacks.TryRemove(senderId, out long targetId))
        {
            var photo = message.Photo.Last();

            await bot.SendPhotoAsync(
                chatId: targetId,
                photo: InputFile.FromFileId(photo.FileId),
                caption: "✅ Вот твоё фото от администратора.",
                cancellationToken: cancellationToken
            );

            await bot.SendTextMessageAsync(
                chatId: AdminId,
                text: $"Фото отправлено игроку {targetId}.",
                cancellationToken: cancellationToken
            );
            return;
        }

        // Если нет команды — игнорируем фото админа
        return;
    }

    // 3. Фото от обычного пользователя
    if (message.Photo != null && message.Photo.Length > 0)
    {
        // Не реагируем на фото от админа (чтобы не пересылать свои же)
        if (senderId == AdminId)
            return;

        var photo = message.Photo.Last(); // самое большое фото
        string fileId = photo.FileId;

        // Сохраняем соответствие: игрок → фото
        userPhotoMap[senderId] = fileId;

        // Пересылаем фото админу
        await bot.SendTextMessageAsync(
            chatId: AdminId,
            text: $"📸 Игрок `{senderId}` прислал фото.",
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );

        await bot.SendPhotoAsync(
            chatId: AdminId,
            photo: InputFile.FromFileId(fileId),
            caption: $"Для ответа используй команду:\n`/sendback {senderId}`",
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );

        return;
    }

    // 4. /start — показать кнопку карты
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
            caption: "Добро пожаловать в TonRoad!\nЕсли у вас Аффон потяите экран к верху и нажмите кнопку ниже, в добрый путь.",
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
