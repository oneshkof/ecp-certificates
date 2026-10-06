using System;
using System.Collections.Generic;

namespace WindowsFormsApp1.Models
{
    public class NotificationSettings
    {
        public bool Enabled { get; set; }

        public bool CheckOnStartup { get; set; } = true;

        public int CheckIntervalMinutes { get; set; } = 1440; // раз в сутки

        public string TelegramBotToken { get; set; } = "";
        public string TelegramChatId { get; set; } = "";
        public List<Subscriber> Subscribers { get; set; } = new List<Subscriber>();

        public void AddOrUpdateSubscriber(Subscriber subscriber)
        {
            if (subscriber == null || string.IsNullOrWhiteSpace(subscriber.ChatId))
            {
                return;
            }

            Subscribers ??= new List<Subscriber>();
            var existing = Subscribers.Find(s => s.ChatId == subscriber.ChatId);
            if (existing == null)
            {
                Subscribers.Add(subscriber);
            }
            else
            {
                existing.Username = subscriber.Username;
                existing.DisplayName = subscriber.DisplayName;
            }
        }

        public List<int> Thresholds { get; set; } = new List<int> { 30, 14, 7 };

        public bool IsReadyToSend =>
            Enabled &&
            !string.IsNullOrWhiteSpace(TelegramBotToken);

        public List<string> GetAllRecipients()
        {
            var result = new List<string>();

            if (Subscribers != null)
            {
                foreach (var subscriber in Subscribers)
                {
                    if (!string.IsNullOrWhiteSpace(subscriber.ChatId) && !result.Contains(subscriber.ChatId))
                    {
                        result.Add(subscriber.ChatId);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(TelegramChatId) && !result.Contains(TelegramChatId))
            {
                result.Add(TelegramChatId);
            }

            return result;
        }

        public NotificationSettings Clone()
        {
            return new NotificationSettings
            {
                Enabled = Enabled,
                CheckOnStartup = CheckOnStartup,
                CheckIntervalMinutes = CheckIntervalMinutes,
                TelegramBotToken = TelegramBotToken,
                TelegramChatId = TelegramChatId,
                Subscribers = Subscribers != null
                    ? Subscribers.ConvertAll(s => s.Clone())
                    : new List<Subscriber>(),
                Thresholds = new List<int>(Thresholds)
            };
        }
    }
}