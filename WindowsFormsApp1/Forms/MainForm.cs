using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp1.Data;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Forms
{
    public class MainForm : Form
    {
        private readonly CertificateRepository _certificateRepo = new CertificateRepository();
        private readonly RequestRepository _requestRepo = new RequestRepository();
        private DataGridView _gridCertificates;
        private DataGridView _gridRequests;
        private DataTable _certificatesTable;
        private DataTable _requestsTable;
        private ToolStripStatusLabel _certStatusLabel;
        private readonly Timer _notifyTimer = new Timer();
        private NotificationSettings _notificationSettings;

        public MainForm()
        {
            Text = "Учёт сертификатов ЭЦП";
            Width = 1100;
            Height = 650;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(800, 500);
            BuildUi();
            LoadCertificates();
            LoadRequests();
            SetupNotifications();
        }

        private void SetupNotifications()
        {
            _notificationSettings = NotificationSettingsStore.Load();
            _notifyTimer.Tick += async (s, e) => await RunNotificationCheckAsync(false);
            ApplyTimerInterval();

            // Проверка при запуске — после того, как окно отрисовано.
            Shown += async (s, e) =>
            {
                if (_notificationSettings.Enabled && _notificationSettings.CheckOnStartup)
                {
                    await RunNotificationCheckAsync(false);
                }
            };
            FormClosing += (s, e) => _notifyTimer.Stop();
        }

        private void ApplyTimerInterval()
        {
            _notifyTimer.Stop();
            if (_notificationSettings != null && _notificationSettings.Enabled)
            {
                int minutes = Math.Max(1, _notificationSettings.CheckIntervalMinutes);
                _notifyTimer.Interval = minutes * 60 * 1000;
                _notifyTimer.Start();
            }
        }

        private void BuildUi()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var certPage = new TabPage("Реестр выданных ЭЦП");
            var requestPage = new TabPage("Заявки на получение ЭЦП");
            _gridCertificates = CreateGrid();
            _gridRequests = CreateGrid();
            certPage.Controls.Add(BuildCertificateLayout());
            requestPage.Controls.Add(BuildRequestLayout());
            tabs.TabPages.Add(certPage);
            tabs.TabPages.Add(requestPage);
            Controls.Add(tabs);
        }

        private static DataGridView CreateGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };
        }

        private Control BuildCertificateLayout()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8)
            };
            var btnAdd = new Button { Text = "Добавить", Width = 110 };
            var btnEdit = new Button { Text = "Редактировать", Width = 120 };
            var btnDelete = new Button { Text = "Удалить", Width = 100 };
            var btnCheck = new Button { Text = "Проверить сейчас", Width = 140 };
            var btnNotifications = new Button { Text = "Настройки уведомлений", Width = 170 };
            var btnSendNow = new Button { Text = "Разослать уведомления", Width = 170 };
            var btnRefresh = new Button { Text = "Обновить", Width = 100 };
            var btnExport = new Button { Text = "Экспорт в Excel", Width = 140 };
            btnAdd.Click += (s, e) => AddCertificate();
            btnEdit.Click += (s, e) => EditCertificate();
            btnDelete.Click += (s, e) => DeleteCertificate();
            btnCheck.Click += (s, e) => CheckNow();
            btnNotifications.Click += (s, e) => OpenNotificationSettings();
            btnSendNow.Click += async (s, e) => await RunNotificationCheckAsync(true);
            btnRefresh.Click += (s, e) => LoadCertificates();
            btnExport.Click += (s, e) => ExportCertificates();

            toolbar.Controls.AddRange(new Control[]
            {
                btnAdd, btnEdit, btnDelete, btnCheck, btnSendNow, btnNotifications, btnRefresh, btnExport
            });

            // Строка состояния снизу.
            var statusStrip = new StatusStrip { SizingGrip = false };
            _certStatusLabel = new ToolStripStatusLabel { Text = "Всего сотрудников: 0" };
            statusStrip.Items.Add(_certStatusLabel);
            panel.Controls.Add(_gridCertificates);
            panel.Controls.Add(statusStrip);
            panel.Controls.Add(toolbar);
            return panel;
        }

        private Control BuildRequestLayout()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8)
            };
            var btnAdd = new Button { Text = "Добавить", Width = 110 };
            var btnEdit = new Button { Text = "Редактировать", Width = 120 };
            var btnDelete = new Button { Text = "Удалить", Width = 100 };
            var btnRefresh = new Button { Text = "Обновить", Width = 100 };
            var btnExport = new Button { Text = "Экспорт в Excel", Width = 140 };
            btnAdd.Click += (s, e) => AddRequest();
            btnEdit.Click += (s, e) => EditRequest();
            btnDelete.Click += (s, e) => DeleteRequest();
            btnRefresh.Click += (s, e) => LoadRequests();
            btnExport.Click += (s, e) => ExportRequests();
            toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh, btnExport });
            panel.Controls.Add(_gridRequests);
            panel.Controls.Add(toolbar);
            return panel;
        }

        // ------------------------- Реестр ЭЦП -------------------------

        private void LoadCertificates()
        {
            _certificatesTable = new DataTable();
            _certificatesTable.Columns.Add("Id", typeof(long));
            _certificatesTable.Columns.Add("ФИО", typeof(string));
            _certificatesTable.Columns.Add("Подразделение", typeof(string));
            _certificatesTable.Columns.Add("№ сертификата", typeof(string));
            _certificatesTable.Columns.Add("Логин к хранилищу", typeof(string));
            _certificatesTable.Columns.Add("Пароль к хранилищу", typeof(string));
            _certificatesTable.Columns.Add("Пароль от ЭЦП", typeof(string));
            _certificatesTable.Columns.Add("Дата выдачи", typeof(DateTime));
            _certificatesTable.Columns.Add("Дата окончания", typeof(DateTime));
            _certificatesTable.Columns.Add("Осталось дней", typeof(int));
            _certificatesTable.Columns.Add("Комментарий", typeof(string));
            _certificatesTable.Columns.Add("Telegram", typeof(string));
            int total = 0;
            int expired = 0;
            int expiring = 0;

            foreach (var cert in _certificateRepo.GetAll())
            {
                total++;
                if (cert.DaysLeft < 0)
                {
                    expired++;
                }
                else if (cert.DaysLeft <= DaysWarningThreshold)
                {
                    expiring++;
                }

                var row = _certificatesTable.NewRow();
                row["Id"] = cert.Id;
                row["ФИО"] = cert.FullName;
                row["Подразделение"] = cert.Department;
                row["№ сертификата"] = cert.SerialNumber;
                row["Логин к хранилищу"] = cert.StoreLogin;
                row["Пароль к хранилищу"] = cert.StorePassword;
                row["Пароль от ЭЦП"] = cert.CertPassword;
                row["Дата выдачи"] = cert.IssueDate;
                row["Дата окончания"] = cert.ExpiryDate;
                row["Осталось дней"] = cert.DaysLeft;
                row["Комментарий"] = cert.Comment;
                row["Telegram"] = string.IsNullOrWhiteSpace(cert.TelegramUsername)
                    ? ""
                    : "@" + cert.TelegramUsername;
                _certificatesTable.Rows.Add(row);
            }

            _gridCertificates.DataSource = _certificatesTable;
            ApplyCertificateColumnSetup();
            UpdateCertificateStatus(total, expired, expiring);
        }

        private const int DaysWarningThreshold = 30;

        private void UpdateCertificateStatus(int total, int expired, int expiring)
        {
            if (_certStatusLabel == null)
            {
                return;
            }

            _certStatusLabel.Text =
                $"Всего сотрудников: {total}   |   Просрочено: {expired}   |   " +
                $"Истекает в течение {DaysWarningThreshold} дней: {expiring}";
        }

        private void CheckNow()
        {
            var expiring = new List<Certificate>();
            foreach (var cert in _certificateRepo.GetAll())
            {
                if (cert.DaysLeft <= DaysWarningThreshold)
                {
                    expiring.Add(cert);
                }
            }

            if (expiring.Count == 0)
            {
                MessageBox.Show(
                    $"Сертификатов, истекающих в течение {DaysWarningThreshold} дней, не найдено.",
                    "Проверка ЭЦП", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var lines = new List<string>();
            foreach (var cert in expiring)
            {
                string when = cert.DaysLeft < 0
                    ? $"просрочен на {-cert.DaysLeft} дн."
                    : $"осталось {cert.DaysLeft} дн.";
                lines.Add($"• {cert.FullName} — {cert.SerialNumber} ({when}, до {cert.ExpiryDate:dd.MM.yyyy})");
            }

            MessageBox.Show(
                $"Сертификаты, истекающие в течение {DaysWarningThreshold} дней " +
                $"({expiring.Count}):\n\n" + string.Join("\n", lines),
                "Проверка ЭЦП", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void OpenNotificationSettings()
        {
            using var dialog = new NotificationSettingsForm(_notificationSettings);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _notificationSettings = NotificationSettingsStore.Load();
                ApplyTimerInterval();
                MessageBox.Show(
                    _notificationSettings.Enabled
                        ? "Настройки уведомлений сохранены и включены."
                        : "Настройки уведомлений сохранены. Уведомления выключены.",
                    "Настройки уведомлений", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async System.Threading.Tasks.Task RunNotificationCheckAsync(bool showResult)
        {
            if (_notificationSettings == null || !_notificationSettings.IsReadyToSend)
            {
                if (showResult)
                {
                    MessageBox.Show(
                        "Уведомления не настроены. Откройте «Настройки уведомлений», " +
                        "укажите токен бота и Chat ID и включите уведомления.",
                        "Уведомления", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            var service = new NotificationService(_certificateRepo, _notificationSettings);
            var result = await service.RunCheckAsync();

            if (showResult)
            {
                var text = $"Проверка завершена.\n\nОтправлено: {result.Sent}\n" +
                           $"Пропущено: {result.Skipped}\nОшибок: {result.Failed}";
                if (!string.IsNullOrEmpty(result.LastError))
                {
                    text += "\n\nПоследняя ошибка: " + result.LastError;
                }
                MessageBox.Show(text, "Уведомления", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ApplyCertificateColumnSetup()
        {
            if (_gridCertificates.Columns.Count == 0)
            {
                return;
            }

            _gridCertificates.Columns["Id"].Visible = false;
            _gridCertificates.Columns["Дата выдачи"].DefaultCellStyle.Format = "dd.MM.yyyy";
            _gridCertificates.Columns["Дата окончания"].DefaultCellStyle.Format = "dd.MM.yyyy";

            // Задаём порядок столбцов как в образце.
            string[] order =
            {
                "ФИО", "Подразделение", "№ сертификата", "Дата выдачи",
                "Дата окончания", "Осталось дней", "Telegram", "Комментарий",
                "Логин к хранилищу", "Пароль к хранилищу", "Пароль от ЭЦП"
            };
            for (int i = 0; i < order.Length; i++)
            {
                if (_gridCertificates.Columns.Contains(order[i]))
                {
                    _gridCertificates.Columns[order[i]].DisplayIndex = i;
                }
            }

            _gridCertificates.CellFormatting -= CertificateCellFormatting;
            _gridCertificates.CellFormatting += CertificateCellFormatting;
        }

        private void CertificateCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_gridCertificates.Columns[e.ColumnIndex].Name != "Осталось дней" || e.Value == null)
            {
                return;
            }

            int days = Convert.ToInt32(e.Value);
            if (days < 0)
            {
                e.CellStyle.ForeColor = Color.White;
                e.CellStyle.BackColor = Color.Firebrick;
                e.CellStyle.SelectionBackColor = Color.Firebrick;
            }
            else if (days <= 30)
            {
                e.CellStyle.ForeColor = Color.Black;
                e.CellStyle.BackColor = Color.Khaki;
                e.CellStyle.SelectionBackColor = Color.Khaki;
            }
        }

        private Certificate GetSelectedCertificate()
        {
            if (_gridCertificates.CurrentRow == null)
            {
                return null;
            }

            var row = ((DataRowView)_gridCertificates.CurrentRow.DataBoundItem).Row;
            long id = Convert.ToInt64(row["Id"]);

            // Берём полную запись из БД: в таблице нет служебных полей
            // (NotifiedThresholds, TelegramChatId), которые нужно сохранить.
            return _certificateRepo.GetById(id);
        }

        private void AddCertificate()
        {
            using var dialog = new CertificateEditForm(null, _certificateRepo.GetDepartments());
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _certificateRepo.Add(dialog.Result);
            LoadCertificates();
        }

        private void EditCertificate()
        {
            var selected = GetSelectedCertificate();
            if (selected == null)
            {
                MessageBox.Show("Выберите запись для редактирования.", "Реестр ЭЦП",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new CertificateEditForm(selected, _certificateRepo.GetDepartments());
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _certificateRepo.Update(dialog.Result);
            LoadCertificates();
        }

        private void DeleteCertificate()
        {
            var selected = GetSelectedCertificate();
            if (selected == null)
            {
                MessageBox.Show("Выберите запись для удаления.", "Реестр ЭЦП",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить запись для «{selected.FullName}» ({selected.SerialNumber})?",
                "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _certificateRepo.Delete(selected.Id);
            LoadCertificates();
        }

        private void ExportCertificates()
        {
            if (_certificatesTable == null || _certificatesTable.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Экспорт",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var exportTable = _certificatesTable.Copy();
            exportTable.Columns.Remove("Id");
            exportTable.Columns["Дата выдачи"].ColumnName = "Дата выдачи";
            exportTable.Columns["Дата окончания"].ColumnName = "Дата окончания";
            var path = ExcelExporter.Export(exportTable, "Реестр_ЭЦП");
            if (path != null)
            {
                MessageBox.Show($"Данные выгружены в файл:\n{path}", "Экспорт завершён",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ------------------------- Заявки -------------------------

        private void LoadRequests()
        {
            _requestsTable = new DataTable();
            _requestsTable.Columns.Add("Id", typeof(long));
            _requestsTable.Columns.Add("ФИО сотрудника", typeof(string));
            _requestsTable.Columns.Add("Номер заявки", typeof(string));
            _requestsTable.Columns.Add("Тип получения", typeof(string));
            _requestsTable.Columns.Add("Дата создания", typeof(DateTime));

            foreach (var request in _requestRepo.GetAll())
            {
                var row = _requestsTable.NewRow();
                row["Id"] = request.Id;
                row["ФИО сотрудника"] = request.FullName;
                row["Номер заявки"] = request.RequestNumber;
                row["Тип получения"] = request.RequestType;
                row["Дата создания"] = request.CreatedDate;
                _requestsTable.Rows.Add(row);
            }

            _gridRequests.DataSource = _requestsTable;
            if (_gridRequests.Columns.Count > 0)
            {
                _gridRequests.Columns["Id"].Visible = false;
                _gridRequests.Columns["Дата создания"].DefaultCellStyle.Format = "dd.MM.yyyy";
            }
        }

        private Request GetSelectedRequest()
        {
            if (_gridRequests.CurrentRow == null)
            {
                return null;
            }

            var row = ((DataRowView)_gridRequests.CurrentRow.DataBoundItem).Row;
            return new Request
            {
                Id = Convert.ToInt64(row["Id"]),
                FullName = Convert.ToString(row["ФИО сотрудника"]),
                RequestNumber = Convert.ToString(row["Номер заявки"]),
                RequestType = Convert.ToString(row["Тип получения"]),
                CreatedDate = row["Дата создания"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(row["Дата создания"])
            };
        }

        private void AddRequest()
        {
            using var dialog = new RequestEditForm(null);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (_requestRepo.NumberExists(dialog.Result.RequestNumber))
            {
                MessageBox.Show("Заявка с таким номером уже существует.", "Проверка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _requestRepo.Add(dialog.Result);
            LoadRequests();
        }

        private void EditRequest()
        {
            var selected = GetSelectedRequest();
            if (selected == null)
            {
                MessageBox.Show("Выберите заявку для редактирования.", "Заявки",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new RequestEditForm(selected);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (_requestRepo.NumberExists(dialog.Result.RequestNumber, dialog.Result.Id))
            {
                MessageBox.Show("Заявка с таким номером уже существует.", "Проверка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _requestRepo.Update(dialog.Result);
            LoadRequests();
        }

        private void DeleteRequest()
        {
            var selected = GetSelectedRequest();
            if (selected == null)
            {
                MessageBox.Show("Выберите заявку для удаления.", "Заявки",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить заявку №{selected.RequestNumber} для «{selected.FullName}»?",
                "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _requestRepo.Delete(selected.Id);
            LoadRequests();
        }

        private void ExportRequests()
        {
            if (_requestsTable == null || _requestsTable.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Экспорт",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var exportTable = _requestsTable.Copy();
            exportTable.Columns.Remove("Id");
            var path = ExcelExporter.Export(exportTable, "Заявки_ЭЦП");
            if (path != null)
            {
                MessageBox.Show($"Данные выгружены в файл:\n{path}", "Экспорт завершён",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}