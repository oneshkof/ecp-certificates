using System;

namespace WindowsFormsApp1.Models
{
    public class Certificate
    {
        public long Id { get; set; }

        public string FullName { get; set; } = "";
        public string Department { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string StoreLogin { get; set; } = "";
        public string StorePassword { get; set; } = "";
        public string CertPassword { get; set; } = "";
        public DateTime IssueDate { get; set; } = DateTime.Today;
        public DateTime ExpiryDate { get; set; } = DateTime.Today;
        public string Comment { get; set; } = "";
        public string TelegramUsername { get; set; } = "";
        public string TelegramChatId { get; set; } = "";
        public string NotifiedThresholds { get; set; } = "";
        public int DaysLeft => (ExpiryDate.Date - DateTime.Today).Days;
    }
}