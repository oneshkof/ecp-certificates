using System;
using System.IO;
using System.Text.Json;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Data
{
    public static class NotificationSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static string SettingsPath =>
            Path.Combine(Path.GetDirectoryName(Database.DbPath) ?? AppContext.BaseDirectory,
                "notification-settings.json");

        public static NotificationSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<NotificationSettings>(json, JsonOptions);
                    if (settings != null)
                    {
                        settings.Thresholds ??= new System.Collections.Generic.List<int> { 30, 14, 7 };
                        return settings;
                    }
                }
            }
            catch
            {
                // Повреждённый файл настроек — используем значения по умолчанию.
            }

            return new NotificationSettings();
        }

        public static void Save(NotificationSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
    }
}