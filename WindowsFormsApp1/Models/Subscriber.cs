using System;

namespace WindowsFormsApp1.Models
{
    public class Subscriber
    {
        public string ChatId { get; set; } = "";
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";

        public string Caption
        {
            get
            {
                var name = string.IsNullOrWhiteSpace(DisplayName) ? "(без имени)" : DisplayName;
                var user = string.IsNullOrWhiteSpace(Username) ? "" : " @" + Username;
                return $"{name}{user}  [id: {ChatId}]";
            }
        }

        public Subscriber Clone() => new Subscriber
        {
            ChatId = ChatId,
            Username = Username,
            DisplayName = DisplayName
        };
    }
}