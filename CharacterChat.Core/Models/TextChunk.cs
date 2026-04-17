using System;

namespace CharacterChat.Models
{
    public class TextChunk
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int ChunkIndex { get; set; }
        public string RawText { get; set; }
        
        // Stored as JSON string array in Database
        public string CharactersMentioned { get; set; }
        
        // Vector blob
        public byte[] EmbeddingVector { get; set; }
        
        public string ChapterHint { get; set; }
    }
}
