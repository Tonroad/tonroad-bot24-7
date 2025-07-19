using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botToken = "7740992334:AAHS2q_ogUV7YW1jPg3b5z9FjLtf6fOojwU";
var adminId = 5959529178; // твой Telegram user ID

var botClient = new TelegramBotClient(botToken);
using var cts = new CancellationTokenSource();

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    new ReceiverOptions(),
    cancellationToken: cts.Token
);

Console.WriteLine("Bot started.");

await Task.Delay(-1, cts.Token);

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;

    // Пользователь прислал фото — пересылаем админу
    if (message.Photo != null && message.From?.Id != adminId)
    {
        var photo = message.Photo.Last(); // лучшее качество
        string caption = $"Photo from user {message.From.Id}";

        // Пересылаем админу (id, username и фото)
        await bot.SendPhotoAsync(
            chatId: adminId,
            photo: photo.FileId,
            caption: caption,
            cancellationToken: cancellationToken
        );
        return;
    }

    // Админ прислал фото с подписью "to: ID" — отправляем пользователю
    if (message.Photo != null && message.From?.Id == adminId)
    {
        string? targetId = null;

        // Ищем user ID в подписи (например: "to: 123456")
        if (!string.IsNullOrEmpty(message.Caption))
        {
            var parts = message.Caption.Split(' ', '\n');
            foreach (var part in parts)
            {
                if (part.StartsWith("to:"))
                    targetId = part.Replace("to:", "").Trim();
            }
        }

        if (long.TryParse(targetId, out long userId))
        {
            await bot.SendPhotoAsync(
                chatId: userId,
                photo: message.Photo.Last().FileId,
                caption: "Photo from admin",
                cancellationToken: cancellationToken
            );
        }
        else
        {
            await bot.SendMessageAsync(
                chatId: adminId,
                text: "Cannot find target user id in caption. Use: to: [user_id]",
                cancellationToken: cancellationToken
            );
        }
        return;
    }

    // Старт — обычная логика
    if (message.Text == "/start")
    {
        await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Send me a photo!"
        );
        return;
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine(exception);
    return Task.CompletedTask;
}
