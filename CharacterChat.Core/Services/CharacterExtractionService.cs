using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CharacterChat.Services
{
    public class CharacterExtractionService
    {
        private readonly HttpClient _httpClient;
        private readonly LlmConfigService _config;

        public CharacterExtractionService(LlmConfigService config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

        public async Task<string> ExtractCharacterNamesAsync(string fullChapterText)
        {
            string prompt = "Extract all proper names of characters heavily featured in this text. Output a JSON object with a single root key 'names' containing an array of strings.";
            
            var requestBody = new
            {
                model = _config.ChatModelName,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = fullChapterText }
                }
            };
            return await FastChatCallAsync(requestBody);
        }

        public async Task<string> ExtractCharacterProfileAsync(string name, string referenceTexts)
        {
            string prompt = $@"Generate a comprehensive deep psychological profile for the character '{name}' based strictly on the provided text.
            Your output must be a pure JSON object matching this schema exactly:
            {{
                ""name"": ""string"", ""aliases"": [""string""], ""personality_traits"": [""string""], 
                ""speech_style"": ""string"", ""emotional_profile"": ""string"", 
                ""knowledge_scope"": [""string""], ""knowledge_gaps"": [""string""], 
                ""relationships"": {{ ""CharacterB"": ""Relation description"" }}, 
                ""sample_dialogue"": [""string""], ""worldview"": ""string"", 
                ""formality_level"": ""string"", ""sentiment_baseline"": ""string"", 
                ""avg_sentence_length"": ""string""
            }}";

            var requestBody = new
            {
                model = _config.ChatModelName,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = referenceTexts }
                }
            };
            
            return await FastChatCallAsync(requestBody);
        }

        private async Task<string> FastChatCallAsync(object requestBody)
        {
            string url = _config.BaseUrl.TrimEnd('/') + "/chat/completions";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

            string json = JsonSerializer.Serialize(requestBody);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseString);
            var content = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return content;
        }
    }
}
