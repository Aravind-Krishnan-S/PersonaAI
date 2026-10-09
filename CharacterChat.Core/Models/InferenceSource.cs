using System.Text.Json.Serialization;

namespace CharacterChat.Models
{
    /// <summary>
    /// Defines the source of LLM inference for persona chat.
    /// </summary>
    public enum InferenceSourceType
    {
        /// <summary>External OpenAI-compatible API (e.g. OpenAI, Anthropic, Groq)</summary>
        Api = 0,

        /// <summary>Local model server running on this machine (e.g. Ollama, LM Studio)</summary>
        LocalModel = 1,

        /// <summary>Remote PersonaAI server on the local network</summary>
        LocalServer = 2
    }

    /// <summary>
    /// Configuration for the active inference source.
    /// Persisted as individual settings via LlmConfigService.
    /// </summary>
    public class InferenceConfig
    {
        /// <summary>Which inference backend to use</summary>
        public InferenceSourceType Source { get; set; } = InferenceSourceType.Api;

        // ── API Mode ──────────────────────────────────────────────
        /// <summary>Base URL for OpenAI-compatible API (e.g. https://api.openai.com/v1)</summary>
        public string ApiBaseUrl { get; set; } = "https://api.openai.com/v1";

        /// <summary>API key / bearer token</summary>
        public string ApiKey { get; set; } = "";

        /// <summary>Model name for chat completions (e.g. gpt-4o-mini)</summary>
        public string ApiModelName { get; set; } = "gpt-4o-mini";

        // ── Local Model Mode ──────────────────────────────────────
        /// <summary>Endpoint for local model server (e.g. http://localhost:11434/v1 for Ollama)</summary>
        public string LocalModelEndpoint { get; set; } = "http://localhost:11434/v1";

        /// <summary>Model name on the local server (e.g. llama3, mistral, phi3)</summary>
        public string LocalModelName { get; set; } = "llama3";

        // ── Local Server Mode ─────────────────────────────────────
        /// <summary>Address of a PersonaAI server on the LAN (e.g. http://192.168.1.5:5142)</summary>
        public string ServerAddress { get; set; } = "";

        /// <summary>
        /// Returns the effective base URL and model name based on current source.
        /// </summary>
        public (string BaseUrl, string ApiKey, string Model) GetEffectiveConfig()
        {
            return Source switch
            {
                InferenceSourceType.Api => (ApiBaseUrl, ApiKey, ApiModelName),
                InferenceSourceType.LocalModel => (LocalModelEndpoint, "", LocalModelName),
                InferenceSourceType.LocalServer => (ServerAddress.TrimEnd('/') + "/api", "", ""),
                _ => (ApiBaseUrl, ApiKey, ApiModelName)
            };
        }
    }
}
