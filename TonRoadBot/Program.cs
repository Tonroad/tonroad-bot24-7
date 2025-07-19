using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

var botClient = new TelegramBotClient("ТОКЕН_ТВОЕГО_БОТА"); // Заменить на токен
var adminId = 5959529178; // Твой Telegram ID

using var cts = new CancellationTokenSource();
var userPhotoMap = new ConcurrentDictionary<long, string>();

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

    long senderId = message.Chat.Id;

    // Обработка команды /start
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
            chatId: senderId,
            photo: InputFile.FromUri("https://raw.githubusercontent.com/tonroad/tonroad-map/main/tonroad_logo.jpg"),
            caption: "Добро пожаловать в TonRoad!\nНажмите кнопку ниже, чтобы открыть карту.",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
        return;
    }

    // Пользователь отправил фото
    if (message.Photo is not null && senderId != adminId)
    {
        var fileId = message.Photo.Last().FileId;
        userPhotoMap[senderId] = fileId;

        // Отправка админу (с сохранением ID в caption)
        await bot.SendPhotoAsync(
            chatId: adminId,
            photo: InputFile.FromFileId(fileId),
            caption: $"Для ответа используй команду:\n/sendback {senderId}",
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );

        Console.WriteLine($"📷 Фото от {senderId} переслано админу");
        return;
    }

    // Админ ответил на сообщение с caption /sendback <id>
    if (message.Photo != null && senderId == adminId && message.ReplyToMessage != null)
    {
        // Пытаемся извлечь userId из caption
        var caption = message.ReplyToMessage.Caption;
        if (!string.IsNullOrEmpty(caption))
        {
            var parts = caption.Split(' ');
            var idPart = parts.LastOrDefault();
            if (long.TryParse(idPart, out long userId))
            {
                var fileId = message.Photo.Last().FileId;

                await bot.SendPhotoAsync(
                    chatId: userId,
                    photo: InputFile.FromFileId(fileId),
                    caption: "✅ Ваше обработанное фото!",
                    cancellationToken: cancellationToken
                );

                Console.WriteLine($"📤 Фото отправлено пользователю {userId}");
                return;
            }
        }

        await bot.SendTextMessageAsync(
            chatId: adminId,
            text: "❌ Не удалось определить ID пользователя. Ответь на сообщение, где есть caption /sendback <id>.",
            cancellationToken: cancellationToken
        );
    }

    // Команда /sendback <id> вручную
    if (message.Text != null && message.Text.StartsWith("/sendback"))
    {
        var parts = message.Text.Split(' ');
        if (parts.Length == 2 && long.TryParse(parts[1], out long targetId))
        {
            if (userPhotoMap.TryGetValue(targetId, out string photoFileId))
            {
                await bot.SendPhotoAsync(
                    chatId: targetId,
                    photo: InputFile.FromFileId(photoFileId),
                    caption: "✅ Ваше фото отправлено обратно!",
                    cancellationToken: cancellationToken
                );

                await bot.SendTextMessageAsync(
                    chatId: adminId,
                    text: $"📤 Фото отправлено пользователю {targetId}.",
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await bot.SendTextMessageAsync(
                    chatId: adminId,
                    text: "❌ Фото не найдено. Возможно, пользователь ещё не отправлял изображение.",
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"❌ Ошибка: {exception.Message}");
    return Task.CompletedTask;
}
