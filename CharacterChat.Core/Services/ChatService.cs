using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CharacterChat.Models;

namespace CharacterChat.Services
{
    public class ChatService
    {
        private readonly HttpClient _httpClient;
        private readonly LlmConfigService _config;
        private readonly InferenceConfig? _inferenceConfig;

        /// <summary>
        /// Legacy constructor: uses LlmConfigService settings directly (API mode only).
        /// </summary>
        public ChatService(LlmConfigService config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5); 
        }

        /// <summary>
        /// Multi-source constructor: routes requests based on InferenceConfig.
        /// </summary>
        public ChatService(LlmConfigService config, InferenceConfig inferenceConfig)
        {
            _config = config;
            _inferenceConfig = inferenceConfig;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Streams chat completion tokens from the configured inference source.
        /// Supports API, Local Model (Ollama/LM Studio), and Local Server modes.
        /// </summary>
        public async IAsyncEnumerable<string> StreamChatAsync(object requestBody)
        {
            if (_inferenceConfig != null && _inferenceConfig.Source == InferenceSourceType.LocalServer)
            {
                // ── Local Server Mode: Call PersonaAI server's /api/chat endpoint ──
                await foreach (var chunk in StreamFromPersonaServerAsync(requestBody))
                {
                    yield return chunk;
                }
                yield break;
            }

            // ── API / Local Model Mode: Both use OpenAI-compatible protocol ──
            string baseUrl;
            string apiKey;
            string modelName;

            if (_inferenceConfig != null)
            {
                var effective = _inferenceConfig.GetEffectiveConfig();
                baseUrl = effective.BaseUrl;
                apiKey = effective.ApiKey;
                modelName = effective.Model;
            }
            else
            {
                // Fallback to legacy LlmConfigService
                baseUrl = _config.BaseUrl;
                apiKey = _config.ApiKey;
                modelName = _config.ChatModelName;
            }

            string url = baseUrl.TrimEnd('/') + "/chat/completions";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            string json = JsonSerializer.Serialize(requestBody);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]") break;

                    JsonDocument? doc = null;
                    string? resultToYield = null;
                    try
                    {
                        doc = JsonDocument.Parse(data);
                        var choices = doc.RootElement.GetProperty("choices");
                        if (choices.GetArrayLength() > 0)
                        {
                            var delta = choices[0].GetProperty("delta");
                            if (delta.TryGetProperty("content", out JsonElement contentElement))
                            {
                                resultToYield = contentElement.GetString();
                            }
                        }
                    }
                    catch
                    {
                        // Handle malformed chunks silently
                    }
                    finally
                    {
                        doc?.Dispose();
                    }

                    if (resultToYield != null)
                    {
                        yield return resultToYield;
                    }
                }
            }
        }

        /// <summary>
        /// Streams chat from a remote PersonaAI server's /api/chat endpoint (SSE).
        /// </summary>
        private async IAsyncEnumerable<string> StreamFromPersonaServerAsync(object requestBody)
        {
            if (_inferenceConfig == null)
                yield break;

            string serverUrl = _inferenceConfig.ServerAddress.TrimEnd('/') + "/api/chat";
            var request = new HttpRequestMessage(HttpMethod.Post, serverUrl);
            
            string json = JsonSerializer.Serialize(requestBody);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]") break;

                    // PersonaAI server sends plain text chunks in SSE format
                    JsonDocument? doc = null;
                    string? resultToYield = null;
                    try
                    {
                        doc = JsonDocument.Parse(data);
                        if (doc.RootElement.TryGetProperty("content", out JsonElement content))
                        {
                            resultToYield = content.GetString();
                        }
                        else if (doc.RootElement.TryGetProperty("choices", out JsonElement choices) &&
                                 choices.GetArrayLength() > 0)
                        {
                            var delta = choices[0].GetProperty("delta");
                            if (delta.TryGetProperty("content", out JsonElement deltaContent))
                            {
                                resultToYield = deltaContent.GetString();
                            }
                        }
                    }
                    catch
                    {
                        // Plain text fallback
                        resultToYield = data;
                    }
                    finally
                    {
                        doc?.Dispose();
                    }

                    if (resultToYield != null)
                    {
                        yield return resultToYield;
                    }
                }
            }
        }
    }
}
