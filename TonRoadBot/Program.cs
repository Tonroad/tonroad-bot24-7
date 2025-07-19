using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    // 👇 Твой бот-токен (разрешённый тобой)
    private static readonly TelegramBotClient botClient = new TelegramBotClient("7683388439:AAE2WgG4QmcZJpl-HwSi0QcToe9q3YNtAEw");

    // 👇 Твой личный Telegram ID (админ)
    private static readonly long adminId = 5959529178;

    // Храним последнее фото от игрока и последнее фото от админа
    private static readonly Dictionary<long, string> lastReceivedPhotos = new Dictionary<long, string>();
    private static string lastPhotoFromAdmin = null;

    static async Task Main()
    {
        botClient.OnMessage += Bot_OnMessage;
        botClient.StartReceiving();
        Console.WriteLine("✅ Бот запущен. Нажми Enter для остановки.");
        Console.ReadLine();
        botClient.StopReceiving();
    }

    private static async void Bot_OnMessage(object sender, MessageEventArgs e)
    {
        var message = e.Message;
        if (message == null) return;

        // Команда /sendback от админа
        if (message.Type == MessageType.Text && message.Text != null)
        {
            if (message.Text.StartsWith("/sendback"))
            {
                string[] parts = message.Text.Split(' ');
                if (parts.Length == 2 && long.TryParse(parts[1], out long targetUserId))
                {
                    if (lastPhotoFromAdmin != null)
                    {
                        await botClient.SendPhotoAsync(
                            chatId: targetUserId,
                            photo: new InputOnlineFile(lastPhotoFromAdmin),
                            caption: "📷 Ваше фото"
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
                            text: "❌ Сначала отправь фото, которое нужно переслать игроку."
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
        }

        // Обработка входящего фото
        if (message.Type == MessageType.Photo)
        {
            var senderId = message.From.Id;
            var fileId = message.Photo[^1].FileId;

            if (senderId != adminId)
            {
                lastReceivedPhotos[senderId] = fileId;

                await botClient.SendTextMessageAsync(
                    chatId: adminId,
                    text: $"📸 Игрок {senderId} прислал фото.\n\nДля ответа используй команду:\n/sendback {senderId}"
                );
            }
            else
            {
                // Фото от админа сохраняем
                lastPhotoFromAdmin = fileId;

                await botClient.SendTextMessageAsync(
                    chatId: adminId,
                    text: "📥 Фото загружено. Теперь отправь /sendback [user_id] чтобы переслать."
                );
            }
        }
    }
}
