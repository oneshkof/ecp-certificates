using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1.Data;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Forms
{
    public class NotificationSettingsForm : Form
    {
        private readonly NotificationSettings _settings;
        private CheckBox _chkEnabled;
        private CheckBox _chkOnStartup;
        private NumericUpDown _numInterval;
        private TextBox _txtToken;
        private TextBox _txtThresholds;
        private ListBox _lstSubscribers;
        private TextBox _txtAddUser;
        private Label _lblStatus;
        private Button _btnSave;
        private Button _btnRefreshSubscribers;
        private Button _btnAddUser;
        private Button _btnRemoveUser;
        private Button _btnTest;

        public NotificationSettings Result { get; private set; }

        public NotificationSettingsForm(NotificationSettings settings)
        {
            _settings = settings ?? new NotificationSettings();
            _settings.Subscribers ??= new System.Collections.Generic.List<Subscriber>();
            Text = "Настройки уведомлений";
            Width = 660;
            Height = 600;
            MinimumSize = new Size(620, 560);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9F);
            BuildUi();
            LoadValues();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // верхние поля
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // получатели
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // статус
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // кнопки

            // --- Верхняя часть: параметры ---
            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2
            };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _chkEnabled = new CheckBox { Text = "Включить уведомления", Dock = DockStyle.Fill };
            _chkOnStartup = new CheckBox { Text = "Проверять при запуске программы", Dock = DockStyle.Fill };
            _numInterval = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 10080, Value = 1440 };
            _txtToken = new TextBox { Dock = DockStyle.Fill };
            _txtThresholds = new TextBox { Dock = DockStyle.Fill };
            AddRow(top, "Состояние:", _chkEnabled);
            AddRow(top, "Запуск:", _chkOnStartup);
            AddRow(top, "Интервал проверки (мин):", _numInterval);
            AddRow(top, "Токен Telegram-бота:", _txtToken);
            AddRow(top, "Пороги (дни, через запятую):", _txtThresholds);

            // --- Средняя часть: список получателей ---
            var recipients = new GroupBox
            {
                Text = "Получатели уведомлений",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            var recLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            recLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            recLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            recLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var recHint = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 40,
                ForeColor = Color.DimGray,
                Text = "Нажмите «Обновить подписчиков» — в список попадут все, кто написал боту " +
                       "/start. Можно добавить человека по @username или вручную по Chat ID."
            };
            _lstSubscribers = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };

            var recButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            _btnRefreshSubscribers = new Button { Text = "Обновить подписчиков", Width = 170 };
            _btnAddUser = new Button { Text = "Добавить по @username / ID", Width = 200 };
            _btnRemoveUser = new Button { Text = "Удалить выбранного", Width = 160 };
            _txtAddUser = new TextBox { Width = 150, PlaceholderText = "@username или ID" };
            _btnRefreshSubscribers.Click += async (s, e) => await RefreshSubscribersAsync();
            _btnAddUser.Click += async (s, e) => await AddUserAsync();
            _btnRemoveUser.Click += (s, e) => RemoveSelectedSubscriber();
            recButtons.Controls.Add(_btnRefreshSubscribers);
            recButtons.Controls.Add(_txtAddUser);
            recButtons.Controls.Add(_btnAddUser);
            recButtons.Controls.Add(_btnRemoveUser);
            recLayout.Controls.Add(recHint, 0, 0);
            recLayout.Controls.Add(_lstSubscribers, 0, 1);
            recLayout.Controls.Add(recButtons, 0, 2);
            recipients.Controls.Add(recLayout);

            // --- Статус ---
            _lblStatus = new Label
            {
                Text = "",
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 26,
                Padding = new Padding(2, 4, 0, 0)
            };

            // --- Кнопки внизу ---
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 8, 0, 0)
            };
            _btnSave = new Button { Text = "Сохранить", Width = 110 };
            var btnCancel = new Button { Text = "Отмена", Width = 100, DialogResult = DialogResult.Cancel };
            _btnTest = new Button { Text = "Отправить тест", Width = 130 };
            _btnSave.Click += OnSaveClick;
            _btnTest.Click += async (s, e) => await SendTestAsync();
            buttons.Controls.Add(_btnSave);
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(_btnTest);
            root.Controls.Add(top, 0, 0);
            root.Controls.Add(recipients, 0, 1);
            root.Controls.Add(_lblStatus, 0, 2);
            root.Controls.Add(buttons, 0, 3);
            Controls.Add(root);
            AcceptButton = _btnSave;
            CancelButton = btnCancel;
        }

        private static void AddRow(TableLayoutPanel layout, string label, Control control)
        {
            layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.Controls.Add(new Label
            {
                Text = label,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            }, 0, layout.RowCount - 1);
            control.Height = 26;
            layout.Controls.Add(control, 1, layout.RowCount - 1);
        }

        private void LoadValues()
        {
            _chkEnabled.Checked = _settings.Enabled;
            _chkOnStartup.Checked = _settings.CheckOnStartup;
            _numInterval.Value = Math.Max(_numInterval.Minimum, Math.Min(_numInterval.Maximum, _settings.CheckIntervalMinutes));
            _txtToken.Text = _settings.TelegramBotToken;
            _txtThresholds.Text = string.Join(", ", _settings.Thresholds);
            RefreshSubscriberList();
        }

        private void RefreshSubscriberList()
        {
            _lstSubscribers.Items.Clear();
            foreach (var subscriber in _settings.Subscribers)
            {
                _lstSubscribers.Items.Add(subscriber.Caption);
            }
        }

        private NotificationSettings Collect()
        {
            var thresholds = new System.Collections.Generic.List<int>();
            foreach (var part in _txtThresholds.Text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out int t) && t > 0)
                {
                    thresholds.Add(t);
                }
            }
            if (thresholds.Count == 0)
            {
                thresholds.AddRange(new[] { 30, 14, 7 });
            }

            _settings.Enabled = _chkEnabled.Checked;
            _settings.CheckOnStartup = _chkOnStartup.Checked;
            _settings.CheckIntervalMinutes = (int)_numInterval.Value;
            _settings.TelegramBotToken = _txtToken.Text.Trim();
            _settings.Thresholds = thresholds;
            return _settings;
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            Result = Collect();
            NotificationSettingsStore.Save(Result);
            DialogResult = DialogResult.OK;
            Close();
        }

        private async Task RefreshSubscribersAsync()
        {
            var token = _txtToken.Text.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                _lblStatus.Text = "Сначала укажите токен бота.";
                return;
            }

            _lblStatus.Text = "Получаем подписчиков...";
            try
            {
                var client = new TelegramClient(token);
                var found = await client.GetSubscribersAsync();
                if (found.Count == 0)
                {
                    _lblStatus.Text = "Никто ещё не написал боту. Напишите боту /start и повторите.";
                    return;
                }

                int added = 0;
                foreach (var subscriber in found)
                {
                    bool exists = _settings.Subscribers.Exists(s => s.ChatId == subscriber.ChatId);
                    _settings.AddOrUpdateSubscriber(subscriber);
                    if (!exists)
                    {
                        added++;
                    }
                }

                RefreshSubscriberList();
                _lblStatus.Text = $"Найдено подписчиков: {found.Count}, добавлено новых: {added}.";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Ошибка: " + ex.Message;
            }
        }

        private async Task AddUserAsync()
        {
            var input = _txtAddUser.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                _lblStatus.Text = "Введите @username или Chat ID.";
                return;
            }

            // Если это число — добавляем напрямую как Chat ID.
            if (long.TryParse(input, out _))
            {
                _settings.AddOrUpdateSubscriber(new Subscriber
                {
                    ChatId = input,
                    DisplayName = "(добавлен вручную)"
                });
                _txtAddUser.Clear();
                RefreshSubscriberList();
                _lblStatus.Text = "Получатель добавлен по Chat ID.";
                return;
            }

            // Иначе ищем username среди тех, кто уже писал боту.
            var username = input.TrimStart('@');
            var token = _txtToken.Text.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                _lblStatus.Text = "Для поиска по @username укажите токен бота.";
                return;
            }

            _lblStatus.Text = "Ищем пользователя...";
            try
            {
                var client = new TelegramClient(token);
                var found = await client.GetSubscribersAsync();
                var match = found.FirstOrDefault(s =>
                    string.Equals(s.Username, username, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    _lblStatus.Text = $"Пользователь @{username} не найден. " +
                                      "Он должен сначала написать боту /start, после этого нажмите «Обновить подписчиков».";
                    return;
                }

                _settings.AddOrUpdateSubscriber(match);
                _txtAddUser.Clear();
                RefreshSubscriberList();
                _lblStatus.Text = $"Пользователь @{username} добавлен.";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Ошибка: " + ex.Message;
            }
        }

        private void RemoveSelectedSubscriber()
        {
            int index = _lstSubscribers.SelectedIndex;
            if (index < 0 || index >= _settings.Subscribers.Count)
            {
                _lblStatus.Text = "Выберите получателя для удаления.";
                return;
            }

            _settings.Subscribers.RemoveAt(index);
            RefreshSubscriberList();
            _lblStatus.Text = "Получатель удалён.";
        }

        private async Task SendTestAsync()
        {
            var settings = Collect();
            var recipients = settings.GetAllRecipients();
            if (string.IsNullOrWhiteSpace(settings.TelegramBotToken) || recipients.Count == 0)
            {
                _lblStatus.Text = "Укажите токен и хотя бы одного получателя.";
                return;
            }

            _lblStatus.Text = "Отправляем тестовое сообщение...";
            int ok = 0, fail = 0;
            try
            {
                var client = new TelegramClient(settings.TelegramBotToken);
                foreach (var chatId in recipients)
                {
                    try
                    {
                        await client.SendMessageAsync(chatId,
                            "✅ <b>Тест уведомлений</b>\n\nМодуль уведомлений успешно настроен.");
                        ok++;
                    }
                    catch
                    {
                        fail++;
                    }
                }
                _lblStatus.Text = $"Отправлено: {ok}, ошибок: {fail}.";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Ошибка: " + ex.Message;
            }
        }
    }
}