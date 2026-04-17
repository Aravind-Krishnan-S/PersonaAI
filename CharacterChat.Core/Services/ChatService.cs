using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CharacterChat.Services
{
    public class ChatService
    {
        private readonly HttpClient _httpClient;
        private readonly LlmConfigService _config;

        public ChatService(LlmConfigService config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5); 
        }

        public async IAsyncEnumerable<string> StreamChatAsync(object requestBody)
        {
            string url = _config.BaseUrl.TrimEnd('/') + "/chat/completions";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

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
    }
}
