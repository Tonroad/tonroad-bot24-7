using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

var botClient = new TelegramBotClient("7740992334:AAHS2q_ogUV7YW1jPg3b5z9FjLtf6fOojwU");

using var cts = new CancellationTokenSource();

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions { AllowedUpdates = { } },
    cancellationToken: cts.Token
);

Console.WriteLine("Bot started. Running until externally stopped.");

// бесконечно ждем пока Railway не перезапустит
await Task.Delay(-1, cts.Token);

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    if (update.Message is { } message && message.Text != null)
    {
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
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine(exception.Message);
    return Task.CompletedTask;
}
