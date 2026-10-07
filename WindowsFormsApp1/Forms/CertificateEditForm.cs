using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Forms
{
    public class CertificateEditForm : Form
    {
        private readonly Certificate _certificate;
        private readonly bool _isNew;
        private TextBox _txtFullName;
        private ComboBox _cmbDepartment;
        private ComboBox _cmbAuthority;
        private ComboBox _cmbType;
        private TextBox _txtSerial;
        private TextBox _txtLogin;
        private TextBox _txtStorePassword;
        private TextBox _txtCertPassword;
        private DateTimePicker _dtIssue;
        private DateTimePicker _dtExpiry;
        private TextBox _txtComment;
        private TextBox _txtTelegramUsername;
        private TextBox _txtTelegramChatId;
        private CheckBox _chkShowPasswords;

        public Certificate Result { get; private set; }

        public CertificateEditForm(Certificate certificate, List<string> departments,
            List<string> authorities, List<string> certificateTypes)
        {
            _isNew = certificate == null;
            _certificate = certificate ?? new Certificate();
            Text = _isNew ? "Новая запись ЭЦП" : "Редактирование записи ЭЦП";
            Width = 520;
            Height = 640;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9F);
            BuildUi(departments, authorities, certificateTypes);
            LoadValues();
        }

        private void BuildUi(List<string> departments, List<string> authorities, List<string> certificateTypes)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(12),
                AutoScroll = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _txtFullName = new TextBox { Dock = DockStyle.Fill };
            _cmbDepartment = CreateCombo(departments);
            _cmbAuthority = CreateCombo(authorities);
            _cmbType = CreateCombo(certificateTypes);
            _txtSerial = new TextBox { Dock = DockStyle.Fill };
            _txtLogin = new TextBox { Dock = DockStyle.Fill };
            _txtStorePassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
            _txtCertPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
            _dtIssue = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            _dtExpiry = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            _txtComment = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
            _txtTelegramUsername = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "@username" };
            _txtTelegramChatId = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "числовой ID (необязательно)" };
            _chkShowPasswords = new CheckBox
            {
                Text = "Показать пароли",
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0)
            };
            _chkShowPasswords.CheckedChanged += (s, e) =>
            {
                _txtStorePassword.UseSystemPasswordChar = !_chkShowPasswords.Checked;
                _txtCertPassword.UseSystemPasswordChar = !_chkShowPasswords.Checked;
            };

            AddRow(layout, "ФИО сотрудника:", _txtFullName);
            AddRow(layout, "Подразделение:", _cmbDepartment);
            AddRow(layout, "Удостоверяющий центр:", _cmbAuthority);
            AddRow(layout, "Тип сертификата:", _cmbType);
            AddRow(layout, "Серийный номер:", _txtSerial);
            AddRow(layout, "Логин к хранилищу:", _txtLogin);
            AddRow(layout, "Пароль к хранилищу:", _txtStorePassword);
            AddRow(layout, "Пароль от ЭЦП:", _txtCertPassword);
            AddRow(layout, "Дата выдачи:", _dtIssue);
            AddRow(layout, "Дата окончания:", _dtExpiry);
            AddRow(layout, "Telegram сотрудника:", _txtTelegramUsername);
            AddRow(layout, "Telegram Chat ID:", _txtTelegramChatId);
            AddRowTall(layout, "Комментарий:", _txtComment);
            AddRow(layout, "", _chkShowPasswords);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 46,
                Padding = new Padding(12, 8, 12, 8)
            };
            var btnCancel = new Button { Text = "Отмена", Width = 100, DialogResult = DialogResult.Cancel };
            var btnSave = new Button { Text = "Сохранить", Width = 100 };
            btnSave.Click += OnSaveClick;
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(btnSave);

            Controls.Add(layout);
            Controls.Add(buttons);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private static ComboBox CreateCombo(List<string> items)
        {
            var combo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown };
            if (items != null)
            {
                combo.Items.AddRange(items.ToArray());
            }
            return combo;
        }

        private static void AddRow(TableLayoutPanel layout, string label, Control control)
        {
            layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, layout.RowCount - 1);
            control.Height = 26;
            layout.Controls.Add(control, 1, layout.RowCount - 1);
        }

        private static void AddRowTall(TableLayoutPanel layout, string label, Control control)
        {
            layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.TopLeft, Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) }, 0, layout.RowCount - 1);
            layout.Controls.Add(control, 1, layout.RowCount - 1);
        }

        private void LoadValues()
        {
            _txtFullName.Text = _certificate.FullName;
            _cmbDepartment.Text = _certificate.Department;
            _cmbAuthority.Text = _certificate.Authority;
            _cmbType.Text = _certificate.CertificateType;
            _txtSerial.Text = _certificate.SerialNumber;
            _txtLogin.Text = _certificate.StoreLogin;
            _txtStorePassword.Text = _certificate.StorePassword;
            _txtCertPassword.Text = _certificate.CertPassword;
            _txtComment.Text = _certificate.Comment;
            _txtTelegramUsername.Text = _certificate.TelegramUsername;
            _txtTelegramChatId.Text = _certificate.TelegramChatId;

            if (_certificate.IssueDate != default)
            {
                _dtIssue.Value = _certificate.IssueDate;
            }
            if (_certificate.ExpiryDate != default)
            {
                _dtExpiry.Value = _certificate.ExpiryDate;
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtFullName.Text))
            {
                MessageBox.Show("Укажите ФИО сотрудника.", "Проверка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtFullName.Focus();
                return;
            }

            if (_dtExpiry.Value.Date < _dtIssue.Value.Date)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты выдачи.", "Проверка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Result = new Certificate
            {
                Id = _certificate.Id,
                FullName = _txtFullName.Text.Trim(),
                Department = _cmbDepartment.Text.Trim(),
                Authority = _cmbAuthority.Text.Trim(),
                CertificateType = _cmbType.Text.Trim(),
                SerialNumber = _txtSerial.Text.Trim(),
                StoreLogin = _txtLogin.Text.Trim(),
                StorePassword = _txtStorePassword.Text,
                CertPassword = _txtCertPassword.Text,
                IssueDate = _dtIssue.Value.Date,
                ExpiryDate = _dtExpiry.Value.Date,
                Comment = _txtComment.Text.Trim(),
                TelegramUsername = _txtTelegramUsername.Text.Trim().TrimStart('@'),
                TelegramChatId = _txtTelegramChatId.Text.Trim()
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
