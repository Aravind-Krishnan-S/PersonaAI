using System;

namespace CharacterChat.Models
{
    public class Character
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string Name { get; set; }
        public string Aliases { get; set; }

        public string ProfileJson { get; set; }
        public DateTime ExtractedAt { get; set; }
    }
}
