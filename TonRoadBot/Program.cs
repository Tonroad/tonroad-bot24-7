using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using System.Collections.Concurrent;

class Program
{
    private static TelegramBotClient botClient;

    // ← ВСТАВЛЕННЫЕ ДАННЫЕ
    private static readonly string token = "7683388439:AAE2WgG4QmcZJpl-HwSi0QcToe9q3YNtAEw";
    private static readonly long adminId = 5398102470;

    // Последние фото (userId -> fileId)
    private static ConcurrentDictionary<long, string> latestPhotos = new();

    static async Task Main(string[] args)
    {
        botClient = new TelegramBotClient(token);
        var me = await botClient.GetMeAsync();
        Console.WriteLine($"✅ Бот {me.Username} запущен!");

        botClient.OnMessage += OnMessageReceived;
        botClient.StartReceiving();

        Console.ReadLine();
        botClient.StopReceiving();
    }

    private static async void OnMessageReceived(object sender, MessageEventArgs e)
    {
        var msg = e.Message;
        if (msg == null) return;

        // Команда /sendback
        if (msg.Text != null && msg.Text.StartsWith("/sendback"))
        {
            if (msg.From.Id != adminId)
            {
                await botClient.SendTextMessageAsync(msg.Chat.Id, "⛔ Только админ может отправлять ответы.");
                return;
            }

            var parts = msg.Text.Split(' ');
            if (parts.Length < 2 || !long.TryParse(parts[1], out long targetUserId))
            {
                await botClient.SendTextMessageAsync(msg.Chat.Id, "❌ Формат: /sendback <userId>");
                return;
            }

            if (latestPhotos.TryGetValue(adminId, out string adminPhoto))
            {
                await botClient.SendPhotoAsync(
                    chatId: targetUserId,
                    photo: new InputOnlineFile(adminPhoto),
                    caption: "📩 Фото от админа.");
                await botClient.SendTextMessageAsync(adminId, $"✅ Фото отправлено игроку {targetUserId}.");
            }
            else
            {
                await botClient.SendTextMessageAsync(adminId, "❌ Нет сохранённого фото для отправки.");
            }

            return;
        }

        // Фото получено
        if (msg.Photo != null && msg.Photo.Length > 0)
        {
            var photo = msg.Photo.OrderByDescending(p => p.FileSize).First();
            latestPhotos[msg.From.Id] = photo.FileId;

            if (msg.From.Id == adminId)
            {
                await botClient.SendTextMessageAsync(adminId, "✅ Фото админа сохранено. Теперь отправь команду /sendback <userId>.");
            }
            else
            {
                string caption = $"📸 Игрок {msg.From.Id} прислал фото.\n\nДля ответа используй команду:\n/sendback {msg.From.Id}";
                await botClient.SendPhotoAsync(adminId, new InputOnlineFile(photo.FileId), caption: caption);
            }
        }
    }
}
