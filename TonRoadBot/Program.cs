using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

var botClient = new TelegramBotClient("7740992334:AAHS2q_ogUV7YW1jPg3b5z9FjLtf6fOojwU");
using var cts = new CancellationTokenSource();

// ID админа
const long adminId = 5959529178;

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions { AllowedUpdates = { } },
    cancellationToken: cts.Token
);

Console.WriteLine("✅ Бот запущен. Ожидает команды /start или фото...");

await Task.Delay(-1, cts.Token);

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    try
    {
        if (update.Message is not { } message)
            return;

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
                chatId: message.Chat.Id,
                photo: InputFile.FromUri("https://raw.githubusercontent.com/tonroad/tonroad-map/main/tonroad_logo.jpg"),
                caption: "Добро пожаловать в TonRoad!\nНажмите кнопку ниже, чтобы открыть карту.",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
            return;
        }

        // Команда для получения ID
        if (message.Text == "/whoami")
        {
            await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"🆔 Ваш Telegram ID: `{message.From.Id}`",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
            return;
        }

        // Пользователь отправил фото
        if (message.Photo != null && message.From.Id != adminId)
        {
            var fileId = message.Photo[^1].FileId;

            await bot.ForwardMessageAsync(
                chatId: adminId,
                fromChatId: message.Chat.Id,
                messageId: message.MessageId,
                cancellationToken: cancellationToken
            );

            Console.WriteLine($"📷 Фото получено от {message.From.Id} и переслано админу.");
            return;
        }

        // Админ отправил фото в ответ (reply)
        if (message.Photo != null && message.From.Id == adminId && message.ReplyToMessage?.ForwardFrom != null)
        {
            var targetUserId = message.ReplyToMessage.ForwardFrom.Id;

            var fileId = message.Photo[^1].FileId;

            await bot.SendPhotoAsync(
                chatId: targetUserId,
                photo: new InputOnlineFile(fileId),
                caption: "Вот ваша обработанная фотография!",
                cancellationToken: cancellationToken
            );

            Console.WriteLine($"📤 Отправлено обработанное фото пользователю {targetUserId}");
            return;
        }

        // Если админ отправил фото, но не ответом
        if (message.Photo != null && message.From.Id == adminId && message.ReplyToMessage == null)
        {
            await bot.SendTextMessageAsync(
                chatId: adminId,
                text: "⚠️ Пожалуйста, используйте функцию \"Ответить\" на пересланное фото, чтобы я знал, кому отправить результат.",
                cancellationToken: cancellationToken
            );
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка: {ex.Message}");
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"❌ Global Error: {exception.Message}");
    return Task.CompletedTask;
}
