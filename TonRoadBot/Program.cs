using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

class Program
{
    private static TelegramBotClient botClient;

    // ⬇️ ВСТАВЬ сюда свой токен
    private static readonly string botToken = "ТВОЙ_ТОКЕН_БОТА";

    // ⬇️ Твой Telegram user ID (админ)
    private static readonly long adminId = 5959529178;

    // Фото от админа (последнее)
    private static string lastPhotoFromAdmin;

    // Последние фото от игроков (не обязательно использовать)
    private static Dictionary<long, string> lastReceivedPhotos = new Dictionary<long, string>();

    static void Main()
    {
        botClient = new TelegramBotClient(botToken);
        botClient.OnMessage += Bot_OnMessage;
        botClient.StartReceiving();
        Console.WriteLine("Bot is running...");
        Console.ReadLine();
        botClient.StopReceiving();
    }

    private static async void Bot_OnMessage(object sender, MessageEventArgs e)
    {
        var message = e.Message;
        if (message == null) return;

        var senderId = message.From.Id;

        // 📦 Команда /sendback
        if (message.Type == MessageType.Text && message.Text.StartsWith("/sendback"))
        {
            string[] parts = message.Text.Split(' ');
            if (parts.Length == 2 && long.TryParse(parts[1], out long targetUserId))
            {
                if (lastPhotoFromAdmin != null)
                {
                    await botClient.SendPhotoAsync(
                        chatId: targetUserId,
                        photo: new InputOnlineFile(lastPhotoFromAdmin),
                        caption: "📷 Вот ваше фото!"
                    );

                    await botClient.SendTextMessageAsync(
                        chatId: adminId,
                        text: $"✅ Фото отправлено игроку {targetUserId}."
                    );
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: adminId,
                        text: "❌ Сначала отправь фото, которое нужно переслать."
                    );
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: adminId,
                    text: "❌ Неверная команда. Пример: /sendback 123456789"
                );
            }

            return;
        }

        // 📸 Фото получено
        if (message.Type == MessageType.Photo)
        {
            var fileId = message.Photo[^1].FileId;

            if (senderId == adminId)
            {
                // Фото от администратора
                lastPhotoFromAdmin = fileId;

                await botClient.SendTextMessageAsync(
                    chatId: adminId,
                    text: "📥 Фото принято. Используй команду /sendback user_id, чтобы отправить игроку."
                );
            }
            else
            {
                // Фото от игрока
                lastReceivedPhotos[senderId] = fileId;

                await botClient.SendPhotoAsync(
                    chatId: adminId,
                    photo: new InputOnlineFile(fileId),
                    caption: $"📸 Игрок {senderId} прислал фото.\n\nДля ответа используй команду:\n/sendback {senderId}"
                );
            }
        }
    }
}
