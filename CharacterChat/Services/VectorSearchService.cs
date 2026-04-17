using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CharacterChat.Models;

namespace CharacterChat.Services
{
    public class VectorSearchService
    {
        // Cosine similarity between two float arrays
        private float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                throw new ArgumentException("Vectors must have the same length");

            float dotProduct = 0;
            float normA = 0;
            float normB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }

            if (normA == 0 || normB == 0) return 0;
            return (float)(dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB)));
        }

        public float[] ByteArrayToFloatArray(byte[] bytes)
        {
            float[] floats = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }

        public byte[] FloatArrayToByteArray(float[] floats)
        {
            byte[] bytes = new byte[floats.Length * 4];
            Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public List<TextChunk> SearchForCharacterContext(List<TextChunk> allChunks, float[] queryEmbedding, string characterName, int topK = 5)
        {
            var results = new List<(TextChunk Chunk, float Score)>();

            foreach (var chunk in allChunks)
            {
                // Strict isolation: only consider chunks where the character is actually mentioned
                var mentionedBase = chunk.CharactersMentioned;
                if (string.IsNullOrEmpty(mentionedBase) || mentionedBase == "[]") continue;
                
                try 
                {
                    var namesMentioned = JsonSerializer.Deserialize<List<string>>(mentionedBase);
                    if (namesMentioned == null || !namesMentioned.Any(n => n.Equals(characterName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue; // Skip chunk if character not here
                    }
                }
                catch { continue; } // Fallback

                var chunkVector = ByteArrayToFloatArray(chunk.EmbeddingVector);
                float score = CosineSimilarity(queryEmbedding, chunkVector);
                results.Add((chunk, score));
            }

            // Return top K by score descending
            return results.OrderByDescending(r => r.Score)
                          .Take(topK)
                          .Select(r => r.Chunk)
                          .ToList();
        }
    }
}
