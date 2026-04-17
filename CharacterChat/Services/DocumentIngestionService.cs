using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CharacterChat.Data;
using CharacterChat.Models;

namespace CharacterChat.Services
{
    public class DocumentIngestionService
    {
        private readonly ChunkRepository _chunkRepo;
        private readonly CharacterRepository _charRepo;
        private readonly EmbeddingService _embedService;
        private readonly CharacterExtractionService _extractionService;

        public DocumentIngestionService(ChunkRepository chunkRepo, CharacterRepository charRepo, EmbeddingService embedService, CharacterExtractionService extractionService)
        {
            _chunkRepo = chunkRepo;
            _charRepo = charRepo;
            _embedService = embedService;
            _extractionService = extractionService;
        }

        public async Task IngestFileAsync(int bookId, string filePath, IProgress<int> progress)
        {
            string fullText = "";
            if (filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                fullText = await File.ReadAllTextAsync(filePath);
            }

            int chunkSize = 2000;
            int overlap = 200;
            var chunks = new List<string>();

            for (int i = 0; i < fullText.Length; i += (chunkSize - overlap))
            {
                int length = Math.Min(chunkSize, fullText.Length - i);
                chunks.Add(fullText.Substring(i, length));
                if (i + length >= fullText.Length) break;
            }

            int index = 0;
            foreach (var cText in chunks)
            {
                float[] vector = await _embedService.GetEmbeddingAsync(cText);

                string mentionedJson = "{ \"names\": [] }"; 
                try { mentionedJson = await _extractionService.ExtractCharacterNamesAsync(cText); } catch { }

                var chunk = new TextChunk
                {
                    BookId = bookId,
                    ChunkIndex = index++,
                    RawText = cText,
                    CharactersMentioned = mentionedJson,
                    EmbeddingVector = FloatArrayToByteArray(vector),
                    ChapterHint = $"Part {index}"
                };

                _chunkRepo.SaveChunk(chunk);
                
                if (progress != null)
                {
                    int percentage = (int)((index / (float)chunks.Count) * 100);
                    progress.Report(percentage);
                }
            }
        }

        private byte[] FloatArrayToByteArray(float[] floats)
        {
            byte[] bytes = new byte[floats.Length * 4];
            Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
