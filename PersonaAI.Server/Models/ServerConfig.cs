using System.Text.Json.Serialization;

namespace PersonaAI.Server.Models
{
    /// <summary>
    /// Configuration for the PersonaAI server, loaded from server-config.json.
    /// </summary>
    public class ServerConfig
    {
        [JsonPropertyName("port")]
        public int Port { get; set; } = 5142;

        [JsonPropertyName("maxClients")]
        public int MaxClients { get; set; } = 10;

        [JsonPropertyName("autoStart")]
        public bool AutoStart { get; set; } = false;

        [JsonPropertyName("databasePath")]
        public string DatabasePath { get; set; } = "";
    }
}
