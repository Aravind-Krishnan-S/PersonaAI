using System;

namespace CharacterChat.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public DateTime ImportedAt { get; set; }
    }
}
