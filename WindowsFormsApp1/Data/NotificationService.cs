using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Data
{
    public class NotificationService
    {
        private readonly CertificateRepository _certificateRepo;
        private readonly NotificationSettings _settings;

        public NotificationService(CertificateRepository certificateRepo, NotificationSettings settings)
        {
            _certificateRepo = certificateRepo;
            _settings = settings;
        }

        public class CheckResult
        {
            public int Sent { get; set; }
            public int Failed { get; set; }
            public int Skipped { get; set; }
            public List<string> Messages { get; } = new List<string>();
            public string LastError { get; set; }
        }

        public async Task<CheckResult> RunCheckAsync()
        {
            var result = new CheckResult();

            if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.TelegramBotToken))
            {
                result.LastError = "Уведомления выключены или не указан токен Telegram-бота.";
                return result;
            }

            var client = new TelegramClient(_settings.TelegramBotToken);
            var thresholds = NormalizeThresholds(_settings.Thresholds);

            // Карта известных Telegram-пользователей: username (в нижнем регистре) → chatId.
            // Берём из настроек и (по возможности) из подписчиков бота.
            var usernameToChatId = await BuildUsernameMapAsync(client);
            var certificates = _certificateRepo.GetAll();

            foreach (var cert in certificates)
            {
                int daysLeft = cert.DaysLeft;
                if (daysLeft < 0)
                {
                    continue; // просроченные отдельно не рассылаем
                }

                // Какие пороги уже отправлены по этой записи.
                var alreadySent = ParseThresholds(cert.NotifiedThresholds);
                var dueThreshold = thresholds
                    .Where(t => daysLeft <= t && !alreadySent.Contains(t))
                    .OrderByDescending(t => t)
                    .ToList();

                if (dueThreshold.Count == 0)
                {
                    result.Skipped++;
                    continue;
                }

                // Определяем адресата именно этого сертификата.
                var chatId = ResolveRecipient(cert, usernameToChatId);
                if (string.IsNullOrWhiteSpace(chatId))
                {
                    // У сертификата не привязан Telegram — пропускаем.
                    result.Skipped++;
                    result.Messages.Add($"Пропущен (нет Telegram): {cert.FullName}");
                    continue;
                }

                try
                {
                    var text = BuildMessage(cert, daysLeft);
                    await client.SendMessageAsync(chatId, text).ConfigureAwait(false);

                    foreach (var t in dueThreshold)
                    {
                        alreadySent.Add(t);
                    }
                    _certificateRepo.UpdateNotifiedThresholds(cert.Id, SerializeThresholds(alreadySent));
                    result.Sent++;
                    result.Messages.Add($"Отправлено: {cert.FullName} ({daysLeft} дн.) → чат {chatId}");
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.LastError = ex.Message;
                    result.Messages.Add($"Ошибка для {cert.FullName}: {ex.Message}");
                }
            }

            return result;
        }

        private static string ResolveRecipient(Certificate cert, Dictionary<string, string> usernameToChatId)
        {
            if (!string.IsNullOrWhiteSpace(cert.TelegramChatId))
            {
                return cert.TelegramChatId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(cert.TelegramUsername))
            {
                var username = cert.TelegramUsername.Trim().TrimStart('@').ToLowerInvariant();
                if (usernameToChatId.TryGetValue(username, out var chatId))
                {
                    return chatId;
                }
            }

            return null;
        }

        private async Task<Dictionary<string, string>> BuildUsernameMapAsync(TelegramClient client)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Из настроек (список получателей).
            if (_settings.Subscribers != null)
            {
                foreach (var subscriber in _settings.Subscribers)
                {
                    if (!string.IsNullOrWhiteSpace(subscriber.Username) &&
                        !string.IsNullOrWhiteSpace(subscriber.ChatId))
                    {
                        map[subscriber.Username.Trim().TrimStart('@')] = subscriber.ChatId.Trim();
                    }
                }
            }

            // Из подписчиков бота (кто написал /start).
            try
            {
                var found = await client.GetSubscribersAsync().ConfigureAwait(false);
                foreach (var subscriber in found)
                {
                    if (!string.IsNullOrWhiteSpace(subscriber.Username))
                    {
                        map[subscriber.Username.Trim().TrimStart('@')] = subscriber.ChatId.Trim();
                    }
                }
            }
            catch
            {
                // Сеть/ошибки API не должны ломать проверку — используем то, что есть.
            }

            return map;
        }

        private static string BuildMessage(Certificate cert, int daysLeft)
        {
            var sb = new StringBuilder();
            sb.AppendLine("⚠️ <b>Внимание: срок действия ЭЦП подходит к концу</b>");
            sb.AppendLine();
            sb.AppendLine($"👤 <b>ФИО:</b> {Escape(cert.FullName)}");
            sb.AppendLine($"🏢 <b>Подразделение:</b> {Escape(cert.Department)}");
            sb.AppendLine($"📜 <b>Сертификат:</b> {Escape(cert.SerialNumber)}");
            sb.AppendLine($"📅 <b>Дата окончания:</b> {cert.ExpiryDate:dd.MM.yyyy}");
            sb.AppendLine($"⏳ <b>Осталось дней:</b> {daysLeft}");
            return sb.ToString();
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "—";
            }
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static List<int> NormalizeThresholds(List<int> thresholds)
        {
            if (thresholds == null || thresholds.Count == 0)
            {
                return new List<int> { 30, 14, 7 };
            }
            return thresholds.Where(t => t > 0).Distinct().OrderByDescending(t => t).ToList();
        }

        private static HashSet<int> ParseThresholds(string value)
        {
            var set = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return set;
            }
            foreach (var part in value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out int t))
                {
                    set.Add(t);
                }
            }
            return set;
        }

        private static string SerializeThresholds(IEnumerable<int> thresholds)
        {
            return string.Join(",", thresholds.Distinct().OrderByDescending(t => t));
        }
    }
}