using System;
using System.Text.Json.Serialization;

namespace CharacterChat.Models
{
    /// <summary>
    /// Status model returned by the PersonaAI server health endpoint
    /// and used by the WinUI monitoring dashboard.
    /// </summary>
    public class ServerStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "offline";

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = "";

        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; } = "";

        [JsonPropertyName("port")]
        public int Port { get; set; } = 5142;

        [JsonPropertyName("uptime")]
        public string Uptime { get; set; } = "";

        [JsonPropertyName("uptimeSeconds")]
        public double UptimeSeconds { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("connectedClients")]
        public int ConnectedClients { get; set; }

        [JsonPropertyName("availablePersonas")]
        public int AvailablePersonas { get; set; }

        [JsonPropertyName("totalRequests")]
        public long TotalRequests { get; set; }

        [JsonPropertyName("memoryUsageMb")]
        public double MemoryUsageMb { get; set; }
    }

    /// <summary>
    /// Represents a discovered PersonaAI server on the local network.
    /// </summary>
    public class DiscoveredServer
    {
        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = "";

        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; } = "";

        [JsonPropertyName("port")]
        public int Port { get; set; } = 5142;

        [JsonPropertyName("personaCount")]
        public int PersonaCount { get; set; }

        /// <summary>Display-friendly label</summary>
        [JsonIgnore]
        public string DisplayName => $"{Hostname} ({IpAddress}:{Port}) — {PersonaCount} personas";
    }
}
