using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace WindowsFormsApp1.Data
{
    public static class ExcelExporter
    {
        public static string Export(DataTable table, string defaultFileName)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Книга Excel (*.xlsx)|*.xlsx",
                FileName = defaultFileName,
                Title = "Экспорт в Excel"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(table, SanitizeSheetName(defaultFileName));

            // Оформление заголовков.
            var header = worksheet.Row(1);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
            return dialog.FileName;
        }

        private static string SanitizeSheetName(string name)
        {
            // Имя листа Excel ограничено 31 символом и не может содержать : \ / ? * [ ].
            var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            foreach (var c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name.Length > 31 ? name.Substring(0, 31) : name;
        }
    }
}