using System;
using System.IO;
using Microsoft.Data.Sqlite;
using CharacterChat.Models;

namespace CharacterChat.Data
{
    public class DatabaseService
    {
        private static string AppDataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CharacterChat");

        public static string GetConnectionString(string bookTitle)
        {
            if (!Directory.Exists(AppDataFolder))
                Directory.CreateDirectory(AppDataFolder);

            string safeTitle = string.Join("_", bookTitle.Split(Path.GetInvalidFileNameChars()));
            string dbPath = Path.Combine(AppDataFolder, $"{safeTitle}.db");
            return $"Data Source={dbPath}";
        }

        public static void InitializeDatabase(string bookTitle)
        {
            using var connection = new SqliteConnection(GetConnectionString(bookTitle));
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Books (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    FilePath TEXT NOT NULL,
                    ImportedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS TextChunks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BookId INTEGER NOT NULL,
                    ChunkIndex INTEGER NOT NULL,
                    RawText TEXT NOT NULL,
                    CharactersMentioned TEXT NOT NULL,
                    EmbeddingVector BLOB NOT NULL,
                    ChapterHint TEXT
                );

                CREATE TABLE IF NOT EXISTS Characters (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BookId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Aliases TEXT,
                    ProfileJson TEXT,
                    ExtractedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ChatHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CharacterId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Timestamp TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );
            ";
            command.ExecuteNonQuery();
        }
    }
}
