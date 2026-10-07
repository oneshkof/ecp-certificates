using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Forms
{
    public class RequestEditForm : Form
    {
        private readonly Request _request;
        private readonly bool _isNew;
        private readonly List<string> _statuses;
        private TextBox _txtFullName;
        private TextBox _txtNumber;
        private ComboBox _cmbType;
        private ComboBox _cmbStatus;
        private DateTimePicker _dtCreated;

        public Request Result { get; private set; }

        public RequestEditForm(Request request, List<string> statuses)
        {
            _isNew = request == null;
            _request = request ?? new Request();
            _statuses = statuses ?? new List<string>();
            Text = _isNew ? "Новая заявка" : "Редактирование заявки";
            Width = 440;
            Height = 340;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9F);
            BuildUi();
            LoadValues();
        }

        private void BuildUi()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _txtFullName = new TextBox { Dock = DockStyle.Fill };
            _txtNumber = new TextBox { Dock = DockStyle.Fill };
            _cmbType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbType.Items.AddRange(RequestTypes.All);
            _cmbStatus = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            if (_statuses.Count > 0)
            {
                _cmbStatus.Items.AddRange(_statuses.ToArray());
            }
            _dtCreated = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            AddRow(layout, "ФИО сотрудника:", _txtFullName);
            AddRow(layout, "Номер заявки:", _txtNumber);
            AddRow(layout, "Тип получения:", _cmbType);
            AddRow(layout, "Статус:", _cmbStatus);
            AddRow(layout, "Дата создания:", _dtCreated);

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

        private static void AddRow(TableLayoutPanel layout, string label, Control control)
        {
            layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, layout.RowCount - 1);
            control.Height = 26;
            layout.Controls.Add(control, 1, layout.RowCount - 1);
        }

        private void LoadValues()
        {
            _txtFullName.Text = _request.FullName;
            _txtNumber.Text = _request.RequestNumber;
            _cmbType.SelectedItem = string.IsNullOrEmpty(_request.RequestType) ? RequestTypes.New : _request.RequestType;
            if (_cmbType.SelectedIndex < 0)
            {
                _cmbType.SelectedIndex = 0;
            }
            if (!string.IsNullOrEmpty(_request.Status))
            {
                _cmbStatus.SelectedItem = _request.Status;
            }
            if (_cmbStatus.SelectedIndex < 0 && _cmbStatus.Items.Count > 0)
            {
                _cmbStatus.SelectedIndex = 0;
            }
            if (_request.CreatedDate != default)
            {
                _dtCreated.Value = _request.CreatedDate;
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

            if (string.IsNullOrWhiteSpace(_txtNumber.Text))
            {
                MessageBox.Show("Укажите номер заявки.", "Проверка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtNumber.Focus();
                return;
            }

            Result = new Request
            {
                Id = _request.Id,
                FullName = _txtFullName.Text.Trim(),
                RequestNumber = _txtNumber.Text.Trim(),
                RequestType = _cmbType.SelectedItem?.ToString() ?? RequestTypes.New,
                Status = _cmbStatus.SelectedItem?.ToString() ?? "",
                CreatedDate = _dtCreated.Value.Date
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}