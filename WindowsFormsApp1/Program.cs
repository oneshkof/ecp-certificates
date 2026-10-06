using System;
using System.Windows.Forms;
using WindowsFormsApp1.Data;
using WindowsFormsApp1.Forms;

namespace WindowsFormsApp1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Создаём файл БД и таблицы при первом запуске.
                Database.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось инициализировать базу данных:\n" + ex.Message,
                    "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new MainForm());
        }
    }
}