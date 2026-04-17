using System;

namespace CharacterChat.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int CharacterId { get; set; }
        public string Role { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
