using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

var botClient = new TelegramBotClient("7740992334:AAHS2q_ogUV7YW1jPg3b5z9FjLtf6fOojwU");
using var cts = new CancellationTokenSource();

var lastStartTimes = new ConcurrentDictionary<long, DateTime>(); // userId → last time

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions { AllowedUpdates = { } },
    cancellationToken: cts.Token
);

Console.WriteLine("✅ Bot started. Waiting for /start...");

await Task.Delay(-1, cts.Token);

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    try
    {
        if (update.Message is { } message && message.Text != null)
        {
            if (message.Text == "/start")
            {
                var userId = message.From.Id;
                var now = DateTime.UtcNow;

                // 🔒 Блокируем повторный /start на 10 секунд
                if (lastStartTimes.TryGetValue(userId, out var lastTime))
                {
                    var seconds = (now - lastTime).TotalSeconds;
                    if (seconds < 10)
                    {
                        Console.WriteLine($"⏹ Повторный /start от {userId} через {seconds:F1} сек — игнорируется.");
                        return;
                    }
                }

                lastStartTimes[userId] = now;

                // 🧾 Подробный лог
                Console.WriteLine("------");
                Console.WriteLine($"🟢 /start от ID: {message.From.Id}");
                Console.WriteLine($"👤 Username: @{message.From.Username}");
                Console.WriteLine($"📱 Имя: {message.From.FirstName} {message.From.LastName}");
                Console.WriteLine($"📩 MessageId: {message.MessageId}");
                Console.WriteLine($"🕒 Время: {message.Date.ToLocalTime()}");
                Console.WriteLine("------");

                // 🔘 WebApp-кнопка
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка в обработке /start: {ex.Message}");
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"❌ Global error: {exception.Message}");
    return Task.CompletedTask;
}
