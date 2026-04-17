using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using CharacterChat.Models;
using System.Text.Json;

namespace CharacterChat.Data
{
    public class ChunkRepository
    {
        private readonly string _connectionString;

        public ChunkRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void SaveChunk(TextChunk chunk)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO TextChunks (BookId, ChunkIndex, RawText, CharactersMentioned, EmbeddingVector, ChapterHint)
                VALUES ($bookId, $chunkIndex, $rawText, $characters, $vector, $chapterHint)";
            command.Parameters.AddWithValue("$bookId", chunk.BookId);
            command.Parameters.AddWithValue("$chunkIndex", chunk.ChunkIndex);
            command.Parameters.AddWithValue("$rawText", chunk.RawText);
            command.Parameters.AddWithValue("$characters", chunk.CharactersMentioned ?? "[]");
            command.Parameters.AddWithValue("$vector", chunk.EmbeddingVector);
            command.Parameters.AddWithValue("$chapterHint", chunk.ChapterHint ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }

        public List<TextChunk> GetAllChunks(int bookId)
        {
            var chunks = new List<TextChunk>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, BookId, ChunkIndex, RawText, CharactersMentioned, EmbeddingVector, ChapterHint FROM TextChunks WHERE BookId = $bookId";
            command.Parameters.AddWithValue("$bookId", bookId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                chunks.Add(new TextChunk
                {
                    Id = reader.GetInt32(0),
                    BookId = reader.GetInt32(1),
                    ChunkIndex = reader.GetInt32(2),
                    RawText = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    CharactersMentioned = reader.IsDBNull(4) ? "{}" : reader.GetString(4),
                    EmbeddingVector = (byte[])reader["EmbeddingVector"],
                    ChapterHint = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
            return chunks;
        }
    }
}
