using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

var botClient = new TelegramBotClient("7683388439:AAE2WgG4QmcZJpl-HwSi0QcToe9q3YNtAEw");
var adminId = 5959529178L;
var userLastPhoto = new ConcurrentDictionary<long, string>();

using var cts = new CancellationTokenSource();

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
    cancellationToken: cts.Token
);

Console.WriteLine("Bot started.");
await Task.Delay(-1, cts.Token);

// Обработка апдейтов
async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;

    var userId = message.From.Id;

    // Принято фото от игрока
    if (message.Photo is not null && userId != adminId)
    {
        var largestPhoto = message.Photo.OrderByDescending(p => p.FileSize).First();
        var fileId = largestPhoto.FileId;

        userLastPhoto[userId] = fileId;

        await bot.SendPhotoAsync(
            chatId: adminId,
            photo: InputFile.FromFileId(fileId),
            caption: $"📷 Пришло фото от игрока {userId}.\n\n" +
                     $"Чтобы отправить обработанное фото, пришли его и команду:\n/sendback {userId}",
            cancellationToken: cancellationToken
        );

        return;
    }

    // Обработка команды от админа
    if (message.Text != null && message.Text.StartsWith("/sendback") && userId == adminId)
    {
        var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !long.TryParse(parts[1], out long targetUserId))
        {
            await bot.SendTextMessageAsync(adminId, "❗ Использование: /sendback <user_id>", cancellationToken: cancellationToken);
            return;
        }

        if (!userLastPhoto.ContainsKey(targetUserId))
        {
            await bot.SendTextMessageAsync(adminId, "⚠️ Нет фото от этого пользователя.", cancellationToken: cancellationToken);
            return;
        }

        if (message.Photo is null)
        {
            await bot.SendTextMessageAsync(adminId, "❗ Прикрепи обработанное фото к команде /sendback", cancellationToken: cancellationToken);
            return;
        }

        var photoToSend = message.Photo.OrderByDescending(p => p.FileSize).First().FileId;

        await bot.SendPhotoAsync(
            chatId: targetUserId,
            photo: InputFile.FromFileId(photoToSend),
            caption: "🖼 Вот ваше обработанное фото!",
            cancellationToken: cancellationToken
        );

        await bot.SendTextMessageAsync(adminId, $"✅ Фото отправлено игроку {targetUserId}", cancellationToken: cancellationToken);
        return;
    }

    // Если игрок или админ написал что-то другое
    if (message.Text == "/start")
    {
        var webAppInfo = new WebAppInfo { Url = "https://tonroad-map.vercel.app" };

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithWebApp("🌍 Открыть TonRoad Map", webAppInfo)
        });

        await bot.SendPhotoAsync(
            chatId: userId,
            photo: InputFile.FromUri("https://raw.githubusercontent.com/tonroad/tonroad-map/main/tonroad_logo.jpg"),
            caption: "Добро пожаловать в TonRoad!\nНажмите кнопку ниже, чтобы открыть карту.",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }
}

// Ошибки
Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"Ошибка: {exception.Message}");
    return Task.CompletedTask;
}
