using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace CharacterChat.Services
{
    public class LlmConfigService
    {
        private readonly string _connectionString;

        public LlmConfigService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            
            // Note: Settings table might not exist until a book is created and SQLite init is fully run,
            // but we can assume global settings are in a default App config db.
            // For now, let's just make it robust to missing table:
            command.CommandText = "SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='Settings'";
            long tableExists = (long)command.ExecuteScalar();
            if (tableExists == 0) return defaultValue;

            command.CommandText = "SELECT Value FROM Settings WHERE Key = $key";
            command.Parameters.AddWithValue("$key", key);
            var result = command.ExecuteScalar();
            return result != null ? result.ToString() : defaultValue;
        }

        public void SaveSetting(string key, string value)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Settings (Key TEXT PRIMARY KEY, Value TEXT);
                INSERT INTO Settings (Key, Value) VALUES ($key, $value)
                ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
            command.Parameters.AddWithValue("$key", key);
            command.Parameters.AddWithValue("$value", value);
            command.ExecuteNonQuery();
        }

        public string BaseUrl => GetSetting("BaseUrl", "https://api.openai.com/v1");
        public string ApiKey => GetSetting("ApiKey", "");
        public string ChatModelName => GetSetting("ChatModelName", "gpt-4o-mini");
        public string EmbeddingsModelName => GetSetting("EmbeddingsModelName", "text-embedding-3-small");
    }
}
