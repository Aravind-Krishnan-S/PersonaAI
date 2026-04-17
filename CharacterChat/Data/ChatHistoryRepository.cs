using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using CharacterChat.Models;

namespace CharacterChat.Data
{
    public class ChatHistoryRepository
    {
        private readonly string _connectionString;

        public ChatHistoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void SaveMessage(ChatMessage msg)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ChatHistory (CharacterId, Role, Content, Timestamp)
                VALUES ($characterId, $role, $content, $timestamp)";
            command.Parameters.AddWithValue("$characterId", msg.CharacterId);
            command.Parameters.AddWithValue("$role", msg.Role);
            command.Parameters.AddWithValue("$content", msg.Content);
            command.Parameters.AddWithValue("$timestamp", msg.Timestamp.ToString("o"));
            command.ExecuteNonQuery();
        }

        public List<ChatMessage> GetRecentMessages(int characterId, int limit = 10)
        {
            var messages = new List<ChatMessage>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM (
                    SELECT Id, CharacterId, Role, Content, Timestamp 
                    FROM ChatHistory 
                    WHERE CharacterId = $characterId 
                    ORDER BY Timestamp DESC 
                    LIMIT $limit
                ) ORDER BY Timestamp ASC";
            command.Parameters.AddWithValue("$characterId", characterId);
            command.Parameters.AddWithValue("$limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                messages.Add(new ChatMessage
                {
                    Id = reader.GetInt32(0),
                    CharacterId = reader.GetInt32(1),
                    Role = reader.GetString(2),
                    Content = reader.GetString(3),
                    Timestamp = DateTime.Parse(reader.GetString(4))
                });
            }
            return messages;
        }
    }
}
