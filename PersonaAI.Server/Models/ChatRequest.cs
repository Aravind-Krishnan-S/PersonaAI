using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PersonaAI.Server.Models
{
    /// <summary>
    /// Incoming chat request from a LAN client.
    /// </summary>
    public class ChatRequest
    {
        [JsonPropertyName("personaId")]
        public int PersonaId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("history")]
        public List<ChatHistoryEntry> History { get; set; } = new();
    }

    /// <summary>
    /// A single message in the chat history sent by the client.
    /// </summary>
    public class ChatHistoryEntry
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}
