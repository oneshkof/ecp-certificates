using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Data
{
    public class TelegramClient
    {
        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        private readonly string _token;

        public TelegramClient(string token)
        {
            _token = token?.Trim() ?? "";
        }

        private string BaseUrl => "https://api.telegram.org/bot" + _token;

        public async Task<bool> SendMessageAsync(string chatId, string text)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                throw new InvalidOperationException("Не задан токен Telegram-бота.");
            }
            if (string.IsNullOrWhiteSpace(chatId))
            {
                throw new InvalidOperationException("Не задан идентификатор чата (Chat ID).");
            }

            var payload = new Dictionary<string, string>
            {
                ["chat_id"] = chatId.Trim(),
                ["text"] = text,
                ["parse_mode"] = "HTML"
            };
            using var content = new FormUrlEncodedContent(payload);
            using var response = await Http.PostAsync(BaseUrl + "/sendMessage", content).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ParseError(body, "Не удалось отправить сообщение."));
            }

            return true;
        }

        public async Task<List<Models.Subscriber>> GetSubscribersAsync()
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                throw new InvalidOperationException("Не задан токен Telegram-бота.");
            }

            using var response = await Http.GetAsync(BaseUrl + "/getUpdates").ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ParseError(body, "Не удалось получить обновления бота."));
            }

            var result = new List<Models.Subscriber>();
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("result", out var updates) ||
                updates.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var update in updates.EnumerateArray())
            {
                if (!update.TryGetProperty("message", out var message) ||
                    !message.TryGetProperty("chat", out var chat) ||
                    !chat.TryGetProperty("id", out var idElement))
                {
                    continue;
                }

                var chatId = idElement.ToString();
                if (result.Exists(s => s.ChatId == chatId))
                {
                    continue;
                }

                string username = "";
                if (chat.TryGetProperty("username", out var usernameElement))
                {
                    username = usernameElement.GetString() ?? "";
                }

                string displayName = "";
                if (chat.TryGetProperty("first_name", out var firstNameElement))
                {
                    displayName = firstNameElement.GetString() ?? "";
                }
                if (chat.TryGetProperty("last_name", out var lastNameElement))
                {
                    var lastName = lastNameElement.GetString();
                    if (!string.IsNullOrWhiteSpace(lastName))
                    {
                        displayName = (displayName + " " + lastName).Trim();
                    }
                }

                result.Add(new Models.Subscriber
                {
                    ChatId = chatId,
                    Username = username,
                    DisplayName = displayName
                });
            }

            return result;
        }

        public async Task<string> TryGetChatIdAsync()
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                throw new InvalidOperationException("Не задан токен Telegram-бота.");
            }

            using var response = await Http.GetAsync(BaseUrl + "/getUpdates").ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ParseError(body, "Не удалось получить обновления бота."));
            }

            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("result", out var result) ||
                result.ValueKind != JsonValueKind.Array ||
                result.GetArrayLength() == 0)
            {
                return null;
            }

            // Идём с конца — нужен самый свежий чат.
            for (int i = result.GetArrayLength() - 1; i >= 0; i--)
            {
                var update = result[i];
                if (update.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("chat", out var chat) &&
                    chat.TryGetProperty("id", out var id))
                {
                    return id.ToString();
                }
            }

            return null;
        }

        private static string ParseError(string body, string fallback)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("description", out var description))
                {
                    return fallback + " " + description.GetString();
                }
            }
            catch
            {
                // Игнорируем ошибку разбора — вернём запасной текст.
            }
            return fallback;
        }
    }
}