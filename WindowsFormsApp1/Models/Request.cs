using System;

namespace WindowsFormsApp1.Models
{
    public static class RequestTypes
    {
        public const string New = "Новый";
        public const string Replacement = "Замена";
        public const string DataUpdate = "Обновление данных";
        public static readonly string[] All = { New, Replacement, DataUpdate };
    }

    public class Request
    {
        public long Id { get; set; }

        public string FullName { get; set; } = "";
        public string RequestNumber { get; set; } = "";
        public string RequestType { get; set; } = RequestTypes.New;
        public DateTime CreatedDate { get; set; } = DateTime.Today;
    }
}