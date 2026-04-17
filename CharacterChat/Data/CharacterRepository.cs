using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using CharacterChat.Models;

namespace CharacterChat.Data
{
    public class CharacterRepository
    {
        private readonly string _connectionString;

        public CharacterRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void SaveCharacter(Character character)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Characters (BookId, Name, Aliases, ProfileJson, ExtractedAt)
                VALUES ($bookId, $name, $aliases, $profileJson, $extractedAt)";
            command.Parameters.AddWithValue("$bookId", character.BookId);
            command.Parameters.AddWithValue("$name", character.Name);
            command.Parameters.AddWithValue("$aliases", character.Aliases ?? "[]");
            command.Parameters.AddWithValue("$profileJson", character.ProfileJson);
            command.Parameters.AddWithValue("$extractedAt", character.ExtractedAt.ToString("o"));
            command.ExecuteNonQuery();
        }

        public List<Character> GetCharactersForBook(int bookId)
        {
            var chars = new List<Character>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, BookId, Name, Aliases, ProfileJson, ExtractedAt FROM Characters WHERE BookId = $bookId";
            command.Parameters.AddWithValue("$bookId", bookId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                chars.Add(new Character
                {
                    Id = reader.GetInt32(0),
                    BookId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    Aliases = reader.GetString(3),
                    ProfileJson = reader.GetString(4),
                    ExtractedAt = DateTime.Parse(reader.GetString(5))
                });
            }
            return chars;
        }
    }
}
